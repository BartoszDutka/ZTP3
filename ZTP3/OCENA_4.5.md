# Implementacja wymagań na ocenę 4.5

## ✅ Wymagania funkcjonalne

### ✅ Powiadomienie o rozpoczęciu realizacji zamówienia - integracja z Lab 2

**Implementacja:**
- `INotificationService` - interface w Domain/Interfaces
- `NotificationService` - implementacja w Infrastructure/Services
- Integracja HTTP z Lab2: `POST http://localhost:5000/api/notifications`
- Format zgodny z `CreateNotificationRequest` z Lab2:
  ```csharp
  {
    "Content": "Twoje zamówienie {orderId} o wartości {totalAmount:C} zostało przyjęte do realizacji.",
    "Channel": "Email",
    "Recipient": userId,
    "TimeZone": "UTC",
    "ScheduledAt": DateTime.UtcNow.AddMinutes(1),
    "Priority": 5
  }
  ```

**Lokalizacja plików:**
- `ShoppingCart.Domain/Interfaces/INotificationService.cs`
- `ShoppingCart.Infrastructure/Services/NotificationService.cs`
- `ShoppingCart.Infrastructure/Services/OrderService.cs` (wysyła notyfikacje)

## ✅ Wymagane praktyki inżynieryjne

### ✅ Niezależne moduły koszyka i zamówień

**Separacja modułów:**
1. **IOrderService interface** - jedyny sposób komunikacji Cart → Order
   - Zdefiniowany w `ShoppingCart.Domain/Interfaces/IOrderService.cs`
   - Metoda: `CreateOrderFromCartAsync(cartId, userId, cartItems)`

2. **Brak bezpośredniego dostępu:**
   - `CheckoutCartCommandHandler` nie zna `IOrderRepository`
   - `CheckoutCartCommandHandler` używa tylko `IOrderService`
   - Cart module nie ma dostępu do tabel Order

3. **OrderService jako niezależny moduł:**
   - Zarządza utworzeniem zamówienia
   - Pobiera aktualne ceny z ProductService
   - Wysyła notyfikacje przez NotificationService
   - Może być podmieniony bez zmian w Cart module

**Przykład użycia:**
```csharp
// CheckoutCartCommandHandler.cs - używa tylko IOrderService
public CheckoutCartCommandHandler(
    ICartRepository cartRepository,
    IOrderService orderService) // NIE OrderRepository!

// Tworzenie zamówienia przez interface
var orderId = await _orderService.CreateOrderFromCartAsync(
    cart.Id, cart.UserId, cart.Items, cancellationToken);
```

**Lokalizacja plików:**
- `ShoppingCart.Domain/Interfaces/IOrderService.cs` - interface
- `ShoppingCart.Infrastructure/Services/OrderService.cs` - implementacja
- `ShoppingCart.Application/Handlers/CheckoutCartCommandHandler.cs` - używa interface

### ✅ Integracja z systemem notyfikacji - niezawodna wysyłka

**Mechanizmy niezawodności:**

1. **Retry logic z exponential backoff:**
   ```csharp
   MaxRetries = 3
   Delays: 1s, 2s, 4s (exponential backoff)
   ```

2. **Fire-and-forget:**
   - Awaria notyfikacji NIE blokuje tworzenia zamówienia
   - Notyfikacja wysyłana asynchronicznie w tle
   ```csharp
   _ = Task.Run(async () =>
   {
       try
       {
           await _notificationService.SendOrderCreatedNotificationAsync(...);
       }
       catch { /* Logged */ }
   });
   ```

3. **Logging:**
   - Sukces: `LogInformation` z orderId i userId
   - Każda próba: `LogWarning` z numerem próby
   - Całkowita porażka: `LogError` z exception

**Lokalizacja:**
- `ShoppingCart.Infrastructure/Services/NotificationService.cs` - pełna implementacja retry

## ✅ Atrybuty jakościowe

### ✅ Spójność danych produktowych

**Implementacja:**

1. **Zawsze aktualne ceny:**
   - `GetCartQuery` - pobiera ceny z ProductService przy każdym odczycie
   - `CheckoutCart` - tworzy zamówienie z cenami z momentu checkout
   - Koszyk przechowuje tylko `ProductId` i `Quantity`

2. **Przewidywalne zachowanie:**
   - Cena w koszyku = aktualna cena z ProductService
   - Cena w zamówieniu = cena z momentu checkout
   - Użytkownik widzi rzeczywiste ceny przed potwierdzeniem

**Scenariusz:**
```
1. Dodanie produktu (cena: 100 PLN)
2. Zmiana ceny w Lab1 (nowa: 120 PLN)
3. GET /carts/{id} → pokazuje 120 PLN (aktualna)
4. POST /checkout → Order utworzony z ceną 120 PLN
```

**Lokalizacja:**
- `ShoppingCart.Application/Handlers/GetCartQueryHandler.cs` - pobiera aktualne ceny
- `ShoppingCart.Infrastructure/Services/OrderService.cs` - checkout z aktualnymi cenami

## Konfiguracja

### appsettings.json
```json
{
  "NotificationService": {
    "ApiUrl": "http://localhost:5000/api/notifications"
  }
}
```

### Rejestracja serwisów (Program.cs)
```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient(); // Dla NotificationService
```

## Zachowanie poprzednich funkcjonalności

### ✅ Ocena 3.0
- Wszystkie komendy CQRS (Create, Add, Remove, Checkout)
- Wszystkie query (GetCart)
- Integracja HTTP z Lab1

### ✅ Ocena 3.5
- Clean Architecture (4 warstwy)
- Rezerwacje produktów (15 min)
- Background Service (czyszczenie co 5 min)
- Usuwanie nieaktywnych koszyków

### ✅ Ocena 4.0
- Optimistic Concurrency Control (Version field)
- Retry logic (3 próby, exponential backoff)
- Ochrona przed podwójnym checkout
- HTTP 409 Conflict przy konfliktach

## Podsumowanie zmian

### Nowe pliki:
1. `ShoppingCart.Domain/Interfaces/IOrderService.cs`
2. `ShoppingCart.Domain/Interfaces/INotificationService.cs`
3. `ShoppingCart.Infrastructure/Services/OrderService.cs`
4. `ShoppingCart.Infrastructure/Services/NotificationService.cs`

### Zmodyfikowane pliki:
1. `ShoppingCart.Application/Handlers/CheckoutCartCommandHandler.cs` - używa IOrderService
2. `ShoppingCart.Api/Program.cs` - rejestracja nowych serwisów
3. `ShoppingCart.Api/appsettings.json` - URL systemu notyfikacji
4. `ShoppingCart.Api/appsettings.Development.json` - URL systemu notyfikacji
5. `README.md` - dokumentacja 4.5

### Architektura:
```
CheckoutCartCommandHandler
    ↓ (używa)
IOrderService (interface - separacja modułów)
    ↓ (implementuje)
OrderService
    ├→ IOrderRepository (zapisuje Order)
    ├→ IProductService (pobiera aktualne ceny)
    └→ INotificationService (wysyła powiadomienie z retry)
           ↓
    NotificationService → HTTP POST do Lab2
```

## Testy

### Test separacji modułów:
- CheckoutCartCommandHandler ma tylko dependency: IOrderService
- NIE ma dostępu do: OrderRepository, NotificationService, ProductService
- Możliwość podmienienia OrderService bez zmian w Cart module

### Test notyfikacji:
```bash
# 1. Uruchom Lab2 (opcjonalnie)
cd ZTP2/src/NotificationSystem.Api && dotnet run

# 2. Checkout
curl -X POST http://localhost:5086/api/v1/carts/{cartId}/checkout

# 3. Sprawdź notyfikację
curl http://localhost:5000/api/notifications
```

### Test spójności cen:
```bash
# 1. Dodaj produkt (Lab1 cena: 100)
# 2. Zmień cenę w Lab1 na 150
# 3. GET koszyk - pokaże 150 PLN
# 4. Checkout - zamówienie z ceną 150 PLN
```

## Status: ✅ Wszystkie wymagania 4.5 zrealizowane

Projekt kompiluje się bez błędów: `dotnet build` - powodzenie ✅
