# Shopping Cart System - Ocena 3.5

System koszyka zakupowego zaimplementowany zgodnie z wzorcem CQRS i zasadami czystej architektury.

## Struktura projektu (Clean Architecture)

```
ShoppingCart.Domain/          # Warstwa domenowa - encje, interfejsy
├── Entities/                 # Cart, CartItem, Order, OrderItem
├── Interfaces/               # ICartRepository, IOrderRepository, IProductService
└── Models/                   # Product

ShoppingCart.Application/     # Warstwa aplikacji - Use Cases
├── Commands/                 # CreateCartCommand, AddProductToCartCommand, etc.
├── Queries/                  # GetCartQuery
├── Handlers/                 # Command i Query handlers
└── DTOs/                     # CartDto, CartItemDto

ShoppingCart.Infrastructure/  # Warstwa infrastruktury - implementacje
├── Persistence/              # ShoppingCartDbContext (MongoDB)
├── Repositories/             # CartRepository, OrderRepository
├── Services/                 # ProductService (HTTP client)
└── BackgroundServices/       # CartCleanupService

ShoppingCart.Api/             # Warstwa prezentacji - API
└── Controllers/              # CartsController
```

## Wymagania na 3.5 - ✅ Zrealizowane

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

### ✅ Integracja HTTP z Lab1
- **ProductService** w Infrastructure
- Pobiera dane produktów poprzez HTTP z API Lab1
- Koszyk przechowuje tylko `ProductId` i `Quantity`

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
Dodaje produkt do koszyka (z 15-minutową rezerwacją)
```json
{
  "productId": "123",
  "quantity": 2
}
```

### DELETE /api/v1/carts/{cartId}/items/{productId}?quantity=1
Usuwa lub zmniejsza ilość produktu w koszyku

### POST /api/v1/carts/{cartId}/checkout
Zatwierdza koszyk i tworzy zamówienie

## Mechanizmy automatyczne

1. **Background Service** - działa automatycznie co 5 minut
2. **Rezerwacje produktów** - 15 minut od dodania/modyfikacji
3. **Wygasanie koszyków** - 15 minut braku aktywności
4. **Automatyczne czyszczenie** - usuwa wygasłe pozycje i puste koszyki

## Uruchomienie

```bash
cd ZTP3/ShoppingCart.Api
dotnet run
```

Aplikacja uruchomi się na http://localhost:5086
