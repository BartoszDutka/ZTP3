# Shopping Cart System - Ocena 4.0

System koszyka zakupowego zaimplementowany zgodnie z wzorcem CQRS, zasadami czystej architektury oraz **optimistic concurrency control** dla zapewnienia spójności operacyjnej.

## Struktura projektu (Clean Architecture)

```
ShoppingCart.Domain/          # Warstwa domenowa - encje, interfejsy
├── Entities/                 # Cart (z Version), CartItem, Order, OrderItem
├── Interfaces/               # ICartRepository, IOrderRepository, IProductService
├── Models/                   # Product
└── Exceptions/               # ConcurrencyException

ShoppingCart.Application/     # Warstwa aplikacji - Use Cases
├── Commands/                 # CreateCartCommand, AddProductToCartCommand, etc.
├── Queries/                  # GetCartQuery
├── Handlers/                 # Command i Query handlers z retry logic
└── DTOs/                     # CartDto, CartItemDto

ShoppingCart.Infrastructure/  # Warstwa infrastruktury - implementacje
├── Persistence/              # ShoppingCartDbContext (MongoDB)
├── Repositories/             # CartRepository (z optimistic locking), OrderRepository
├── Services/                 # ProductService (HTTP client)
└── BackgroundServices/       # CartCleanupService

ShoppingCart.Api/             # Warstwa prezentacji - API
└── Controllers/              # CartsController (z obsługą ConcurrencyException)
```

## Wymagania na 4.0 - ✅ Zrealizowane

### ✅ Wymagania funkcjonalne (3.0)
- **Tworzenie koszyka** - `CreateCartCommand`
- **Dodawanie produktów** - `AddProductToCartCommand` z aktualizacją ilości
- **Usuwanie produktów** - `RemoveProductFromCartCommand` (usuwa gdy ilość ≤ 0)
- **Podgląd koszyka** - `GetCartQuery` (zwraca produkty z nazwami, cenami, wartościami)
- **Zatwierdzanie koszyka** - `CheckoutCartCommand` (tworzy Order)

### ✅ Mechanizm blokowania produktów (3.5)
- Każdy produkt dodany do koszyka ma **rezerwację na 15 minut** (`ReservationExpiresAt`)
- Rezerwacja jest **automatycznie przedłużana** przy każdej modyfikacji koszyka
- Background Service **usuwa wygasłe rezerwacje** automatycznie

### ✅ Usuwanie nieaktywnych koszyków (3.5)
- **CartCleanupService** - Background Service uruchamiany co 5 minut
- Usuwa koszyki nieaktywne **dłużej niż 15 minut** (`LastActivityAt`)
- Usuwa pozycje z **wygasłymi rezerwacjami**
- Usuwa **puste koszyki** po wyczyszczeniu pozycji

### ✅ Czysta architektura (3.5)
- **Domain** - bez zależności zewnętrznych, czyste encje
- **Application** - logika biznesowa, używa tylko interfejsów z Domain
- **Infrastructure** - implementacje MongoDB, HTTP client, background services
- **API** - warstwa prezentacji, tylko kontrolery

### ✅ Współbieżność (4.0)
- **Izolacja użytkowników** - każdy koszyk ma `UserId`, operacje nie wpływają na innych
- **Optimistic Locking** - pole `Version` w Cart
- **Atomiczne operacje** - MongoDB ReplaceOne z filtrem na Version
- **Brak utraty danych** - retry logic przy konfliktach (max 3 próby)

### ✅ Spójność operacyjna (4.0)
- **Wersjonowanie dokumentów** - `Cart.Version` inkrementowane przy każdym zapisie
- **Wykrywanie konfliktów** - `ConcurrencyException` gdy Version się nie zgadza
- **Automatyczne retry** - 3 próby z opóźnieniem (100ms, 200ms, 300ms)
- **Ochrona przed podwójnym checkout** - `IsCheckedOut` + optimistic locking
- **Deterministyczna kolejność** - wszystkie operacje z retry zachowują porządek

### ✅ Integracja HTTP z Lab1
- **ProductService** w Infrastructure
- Pobiera dane produktów poprzez HTTP z API Lab1
- Koszyk przechowuje tylko `ProductId` i `Quantity`

## Mechanizmy współbieżności

### Optimistic Locking
```csharp
// Cart.cs
public long Version { get; set; } = 0;

// CartRepository.UpdateAsync
var filter = Builders<Cart>.Filter.And(
    Builders<Cart>.Filter.Eq(c => c.Id, cart.Id),
    Builders<Cart>.Filter.Eq(c => c.Version, currentVersion)
);
```

### Retry Logic
```csharp
// Handler example
var retryCount = 0;
while (retryCount < MaxRetries)
{
    try
    {
        // ... operacja na koszyku
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        return true;
    }
    catch (ConcurrencyException)
    {
        retryCount++;
        if (retryCount >= MaxRetries) throw;
        await Task.Delay(100 * retryCount, cancellationToken);
    }
}
```

### Ochrona checkout
- Tylko jeden checkout może się udać (optimistic locking)
- `IsCheckedOut` ustawiane atomowo z weryfikacją Version
- Przy konflikcie zwracany jest HTTP 409 Conflict

## Konfiguracja

### appsettings.json
```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ShoppingCartDb"
  },
  "ProductServiceUrl": "http://localhost:5220"
}
```

## Endpointy API

### POST /api/v1/carts
Tworzy nowy koszyk
```json
{
  "userId": "user@example.com"
}
```

### GET /api/v1/carts/{cartId}
Pobiera szczegóły koszyka z produktami

### POST /api/v1/carts/{cartId}/items
Dodaje produkt do koszyka (z 15-minutową rezerwacją i retry)
```json
{
  "productId": "123",
  "quantity": 2
}
```
**Odpowiedzi:**
- `200 OK` - sukces
- `409 Conflict` - konflikt współbieżności (retry wyczerpany)

### DELETE /api/v1/carts/{cartId}/items/{productId}?quantity=1
Usuwa lub zmniejsza ilość produktu w koszyku (z retry)

### POST /api/v1/carts/{cartId}/checkout
Zatwierdza koszyk i tworzy zamówienie (z ochroną przed podwójnym checkout)
**Odpowiedzi:**
- `200 OK` - zamówienie utworzone
- `400 Bad Request` - koszyk pusty lub już zatwierdzony
- `409 Conflict` - konflikt podczas checkout

## Mechanizmy automatyczne

1. **Background Service** - działa automatycznie co 5 minut
2. **Rezerwacje produktów** - 15 minut od dodania/modyfikacji
3. **Wygasanie koszyków** - 15 minut braku aktywności
4. **Automatyczne czyszczenie** - usuwa wygasłe pozycje i puste koszyki
5. **Retry logic** - automatyczne ponowienie przy konfliktach (max 3x)
6. **Optimistic locking** - wykrywanie i obsługa konfliktów współbieżności

## Uruchomienie

```bash
# Uruchom MongoDB
docker run -d -p 27017:27017 --name mongodb mongo:latest

# Uruchom aplikację
cd ZTP3/ShoppingCart.Api
dotnet run
```

Aplikacja uruchomi się na http://localhost:5086

## Scenariusze testowe współbieżności

### Test 1: Równoległe dodawanie produktów
```bash
# Terminal 1
curl -X POST http://localhost:5086/api/v1/carts/{cartId}/items \
  -H "Content-Type: application/json" \
  -d '{"productId":"123","quantity":1}'

# Terminal 2 (jednocześnie)
curl -X POST http://localhost:5086/api/v1/carts/{cartId}/items \
  -H "Content-Type: application/json" \
  -d '{"productId":"456","quantity":2}'
```
**Oczekiwany rezultat:** Obie operacje zakończą się sukcesem dzięki retry logic

### Test 2: Podwójny checkout
```bash
# Terminal 1 i 2 jednocześnie
curl -X POST http://localhost:5086/api/v1/carts/{cartId}/checkout
```
**Oczekiwany rezultat:** Tylko jeden checkout się uda, drugi otrzyma 400 Bad Request lub 409 Conflict

