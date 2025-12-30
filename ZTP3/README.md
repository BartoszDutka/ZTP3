# Shopping Cart System - Ocena 4.5

System koszyka zakupowego zaimplementowany zgodnie z wzorcem CQRS, zasadami czystej architektury, **optimistic concurrency control** oraz **integracją z systemem notyfikacji**.

## Struktura projektu (Clean Architecture)

```
ShoppingCart.Domain/          # Warstwa domenowa - encje, interfejsy
├── Entities/                 # Cart (z Version), CartItem, Order, OrderItem
├── Interfaces/               # ICartRepository, IOrderRepository, IProductService
│   ├── IOrderService         # Interface separujący moduły Cart i Order
│   └── INotificationService  # Interface dla notyfikacji
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
├── Services/                 # ProductService (HTTP), OrderService, NotificationService
│   ├── OrderService          # Niezależny moduł zamówień
│   └── NotificationService   # Integracja z Lab2 z retry logic
└── BackgroundServices/       # CartCleanupService

ShoppingCart.Api/             # Warstwa prezentacji - API
└── Controllers/              # CartsController (z obsługą ConcurrencyException)
```

## Wymagania na 4.5 - ✅ Zrealizowane

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

### ✅ Niezależne moduły Cart i Order (4.5)
- **IOrderService interface** - jedyny sposób komunikacji Cart → Order
- **Brak bezpośredniego dostępu** - Cart nie zna OrderRepository
- **OrderService** - niezależny moduł zarządzający zamówieniami
- **Możliwość zastąpienia** - implementacja OrderService może być podmieniona
- **Separacja w CheckoutCartCommandHandler** - używa tylko IOrderService

### ✅ Integracja z systemem notyfikacji Lab2 (4.5)
- **INotificationService** - interface dla wysyłki notyfikacji
- **NotificationService** - implementacja z HTTP client
- **Retry logic** - 3 próby z exponential backoff (1s, 2s, 4s)
- **Fire-and-forget** - awaria notyfikacji nie blokuje tworzenia zamówienia
- **Integracja HTTP** - POST do `http://localhost:5000/api/notifications`
- **Format Lab2** - zgodny z `CreateNotificationRequest` z Lab2

### ✅ Spójność cen produktów (4.5)
- **Zawsze aktualne ceny** - każde zapytanie pobiera ceny z ProductService
- **GetCartQuery** - pokazuje aktualne ceny w momencie odczytu
- **CheckoutCart** - tworzy zamówienie z cenami z momentu checkout
- **Przewidywalne zachowanie** - cena może się zmienić między dodaniem a checkout

### ✅ Integracja HTTP z Lab1
- **ProductService** w Infrastructure
- Pobiera dane produktów poprzez HTTP z API Lab1
- Koszyk przechowuje tylko `ProductId` i `Quantity`

## Mechanizmy współbieżności i separacji modułów

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

### Separacja modułów Cart i Order
```csharp
// CheckoutCartCommandHandler używa tylko IOrderService
public CheckoutCartCommandHandler(
    ICartRepository cartRepository,
    IOrderService orderService) // Nie ma bezpośredniego dostępu do OrderRepository

// OrderService zarządza całą logiką zamówień
var orderId = await _orderService.CreateOrderFromCartAsync(
    cart.Id, cart.UserId, cart.Items, cancellationToken);
```

### Integracja z notyfikacjami (z retry logic)
```csharp
// NotificationService - retry z exponential backoff
var retryCount = 0;
while (retryCount < MaxRetries) // 3 próby
{
    try
    {
        var response = await _httpClient.PostAsJsonAsync(url, request);
        if (response.IsSuccessStatusCode) return;
    }
    catch (Exception ex) { /* log */ }
    
    retryCount++;
    var delayMs = (int)Math.Pow(2, retryCount - 1) * 1000; // 1s, 2s, 4s
    await Task.Delay(delayMs);
}

// Fire-and-forget - nie blokuje tworzenia zamówienia
_ = Task.Run(async () => 
    await _notificationService.SendOrderCreatedNotificationAsync(...));
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
  "ProductServiceUrl": "http://localhost:5220",
  "NotificationService": {
    "ApiUrl": "http://localhost:5000/api/notifications"
  }
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
- **Tworzy zamówienie** z aktualnymi cenami z ProductService
- **Wysyła notyfikację** do systemu Lab2
- **Używa separacji modułów** - CheckoutCart → IOrderService → OrderService

**Odpowiedzi:**
- `200 OK` - zamówienie utworzone, notyfikacja wysłana (fire-and-forget)
- `400 Bad Request` - koszyk pusty lub już zatwierdzony
- `409 Conflict` - konflikt podczas checkout

## Mechanizmy automatyczne

1. **Background Service** - działa automatycznie co 5 minut
2. **Rezerwacje produktów** - 15 minut od dodania/modyfikacji
3. **Wygasanie koszyków** - 15 minut braku aktywności
4. **Automatyczne czyszczenie** - usuwa wygasłe pozycje i puste koszyki
5. **Retry logic** - automatyczne ponowienie przy konfliktach (max 3x)
6. **Optimistic locking** - wykrywanie i obsługa konfliktów współbieżności
7. **Notyfikacje z retry** - 3 próby wysyłki z exponential backoff (1s, 2s, 4s)

## Uruchomienie

```bash
# Uruchom MongoDB
docker run -d -p 27017:27017 --name mongodb mongo:latest

# Uruchom system notyfikacji Lab2 (opcjonalne)
cd ZTP2/src/NotificationSystem.Api
dotnet run

# Uruchom serwis produktowy Lab1
cd ZTP1/ZTP1/ProductManagementAPI
dotnet run

# Uruchom aplikację koszyka
cd ZTP3/ShoppingCart.Api
cd ZTP3/ShoppingCart.Api
dotnet run
```

Aplikacja uruchomi się na http://localhost:5086

**Uwaga:** System notyfikacji (Lab2) jest opcjonalny - zamówienia są tworzone nawet gdy Lab2 nie działa (fire-and-forget).

## Scenariusze testowe

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

### Test 3: Notyfikacja po checkout
```bash
# 1. Utwórz koszyk
curl -X POST http://localhost:5086/api/v1/carts \
  -H "Content-Type: application/json" \
  -d '{"userId":"user@example.com"}'

# 2. Dodaj produkt
curl -X POST http://localhost:5086/api/v1/carts/{cartId}/items \
  -H "Content-Type: application/json" \
  -d '{"productId":"123","quantity":2}'

# 3. Checkout
curl -X POST http://localhost:5086/api/v1/carts/{cartId}/checkout

# 4. Sprawdź notyfikację w Lab2 (jeśli działa)
curl http://localhost:5000/api/notifications
```
**Oczekiwany rezultat:** Zamówienie utworzone, notyfikacja wysłana do Lab2 (z retry jeśli potrzeba)

### Test 4: Spójność cen
```bash
# 1. Dodaj produkt do koszyka (zapamiętana cena: np. 100 PLN)
# 2. Zmień cenę produktu w Lab1
# 3. Pobierz koszyk - pokaże AKTUALNĄ cenę z Lab1
curl -X GET http://localhost:5086/api/v1/carts/{cartId}
# 4. Checkout - zamówienie z AKTUALNĄ ceną
curl -X POST http://localhost:5086/api/v1/carts/{cartId}/checkout
```
**Oczekiwany rezultat:** Koszyk zawsze pokazuje aktualne ceny, zamówienie tworzone z cenami z momentu checkout

### Test 5: Separacja modułów
- CheckoutCartCommandHandler używa tylko **IOrderService** (nie OrderRepository)
- OrderService zarządza utworzeniem Order i wysyłką notyfikacji
- Możliwość podmiany implementacji OrderService bez zmian w Cart module

