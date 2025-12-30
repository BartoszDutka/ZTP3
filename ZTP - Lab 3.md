
# Laboratorium 3 – System koszyka zakupowego

## Podsumowanie

- **Cel**: Celem laboratorium jest zaprojektowanie i implementacja systemu koszyka zakupowego w oparciu o wzorzec CQRS (Command Query Responsibility Segregation). System powinien umożliwiać użytkownikom tworzenie, modyfikację i realizację koszyka zakupowego przy jednoczesnym rozdzieleniu operacji odczytu od operacji zapisu. Interakcja z serwisem produktów odbywa się poprzez protokół HTTP.
- **Minimalny zakres (3.0)**:
  - System umożliwia utworzenie nowego koszyka dla wskazanego użytkownika, dodawanie i usuwanie produktów z koszyka, podgląd zawartości koszyka wraz z łączną wartością oraz realizację koszyka poprzez utworzenie zamówienia.
  - Użytkownik jest identyfikowany za pomocą jednoznacznego identyfikatora (np. GUID, numer indeksu, e-mail). Komunikacja z serwisem produktowym odbywa się wyłącznie przez HTTP.
  - Koszyk przechowuje jedynie niezbędne dane do obsługi procesu zakupowego. Informacje o produktach (nazwa, opis, aktualna cena itp.) są pobierane na żądanie z serwisu produktowego z Laboratorium 1.
- **Dodatki**:
  - **3.5**: Rozszerzenie systemu o mechanizm blokowania produktów przy dodaniu ich do koszyka, mechanizm automatycznego wygasania blokady po piętnastu minutach braku aktywności oraz usuwanie nieaktywnych koszyków. Implementacja architektury opartej na Use Case’ach (czysta architektura) lub innej, wcześniej zaakceptowanej przez prowadzącego.
  - **4.0**: Zapewnienie współbieżności oraz izolacji danych pomiędzy użytkownikami, a także spójności operacyjnej w przypadku równoległych modyfikacji koszyków. Wysłanie powiadomienia o rozpoczęciu realizacji zamówienia oraz zastosowanie czystej, testowalnej logiki aplikacyjnej.
  - **4.5**: Przygotowanie piętnastominutowej prezentacji wraz z demonstracją działania rozwiązania (demo techniczne), po wcześniejszej akceptacji zakresu przez prowadzącego. Dopracowanie integracji z systemem notyfikacji z Laboratorium 2 oraz wyraźne rozdzielenie modułu koszyka od modułu zamówień.
  - **5.0**: Zastosowanie oddzielnych baz danych dla warstwy zapisów i dla warstwy odczytów, wraz z mechanizmem projekcji danych z bazy zapisowej do bazy odczytowej, procedurą odbudowy projekcji oraz podstawowymi mechanizmami monitorowania działania systemu.

## Wprowadzenie i cel

Celem laboratorium jest zaprojektowanie i implementacja systemu koszyka zakupowego w oparciu o wzorzec CQRS. System ma zapewniać izolację operacji użytkowników, spójność operacyjną w obliczu równoległych modyfikacji oraz możliwość poziomego skalowania. Interakcja z zewnętrznym serwisem produktowym odbywa się przez HTTP.  
Docelowo rozwiązanie powinno być zaprojektowane tak, aby możliwe było jego dalsze rozszerzanie (np. o płatności, fakturowanie, dodatkowe kanały komunikacji z użytkownikiem).

---

## Kryteria oceny

### Ocena 2.0

Aby uzyskać ocenę 2.0, rozwiązanie **nie spełnia** minimalnych wymagań laboratorium. Oznacza to w szczególności, że:

- Nie zostało przesłane żadne rozwiązanie albo przesłane rozwiązanie jest nieuruchamialne lub niekompletne w stopniu uniemożliwiającym ocenę funkcjonalności.
- Autor nie potrafi wyjaśnić działania stworzonego kodu (np. podczas rozmowy lub krótkiego omówienia rozwiązania).
- Kod w istotny sposób narusza podstawowe zasady projektowe (SOLID, DRY, KISS) lub nie zachowuje podziału na warstwy aplikacji.

---

### Ocena 3.0

Aby uzyskać ocenę 3.0, rozwiązanie musi spełniać poniższe wymagania.

#### Wymagania funkcjonalne

System powinien realizować następujące funkcje:

- **Dodawanie produktów do koszyka**: System umożliwia dodanie produktu do istniejącego koszyka poprzez przekazanie:
  - identyfikatora koszyka,
  - identyfikatora produktu z serwisu produktowego z Laboratorium 1,
  - żądanej ilości (liczba całkowita dodatnia).
  Dodanie produktu powinno skutkować pojawieniem się nowej pozycji w koszyku lub aktualizacją ilości istniejącej pozycji, jeżeli dany produkt został już wcześniej dodany.
- **Usuwanie produktów z koszyka**: System umożliwia usunięcie produktu z koszyka lub zmniejszenie jego ilości. W przypadku zmniejszania ilości należy jednoznacznie zdefiniować zachowanie, gdy ilość spadnie do zera.
- **Podgląd koszyka**: System udostępnia operację odczytu zawartości koszyka, która zwraca:
  - listę pozycji w koszyku (identyfikator produktu, nazwa, ilość, cena jednostkowa, wartość pozycji),
  - łączną wartość koszyka.  
  Dane o produktach (nazwa, aktualna cena) są pobierane z serwisu produktowego z Laboratorium 1.
- **Zatwierdzanie koszyka**: System umożliwia zatwierdzenie koszyka jako gotowego do zamówienia (np. komenda `CheckoutCart`). W wyniku zatwierdzenia koszyka powinno zostać utworzone zamówienie.

#### Wymagane praktyki inżynieryjne

- **Zastosowanie wzorca CQRS**:  
  - Operacje modyfikujące stan systemu (tworzenie koszyka, dodawanie produktu, usuwanie produktu, zatwierdzanie koszyka) muszą być zaimplementowane jako komendy (np. `AddProductToCartCommand`, `RemoveProductFromCartCommand`, `CheckoutCartCommand`) wraz z odpowiednimi handlerami.
  - Operacje odczytu (podgląd koszyka, lista koszyków użytkownika itp.) muszą być zaimplementowane jako zapytania (np. `GetCartQuery`) wraz z odpowiednimi handlerami.
- **Identyfikacja użytkownika**:  
  Użytkownik jest identyfikowany w systemie poprzez unikalny identyfikator (np. GUID, identyfikator konta, adres e-mail).
- **Zakres danych przechowywanych w koszyku**:  
  Koszyk przechowuje tylko niezbędne informacje, takie jak:
  - identyfikator koszyka,
  - identyfikator użytkownika,
  - pozycje koszyka (identyfikator produktu, ilość oraz – opcjonalnie – cena z momentu dodania, jeżeli jest to częścią przyjętego modelu).  
  Dane opisowe produktów zawsze są pobierane z serwisu produktowego.

---

### Ocena 3.5

Aby uzyskać ocenę 3.5, rozwiązanie musi spełniać wymagania na ocenę 3.0 oraz dodatkowo poniższe wymagania.

#### Wymagania funkcjonalne

- **Usuwanie nieaktywnych koszyków**:  
  System powinien usuwać lub archiwizować koszyki, które są nieaktywne (brak operacji) dłużej niż ustalony czas (np. 15 minut). Mechanizm może być realizowany poprzez:
  - zadanie cykliczne (cron, background job),
  - mechanizm TTL (time-to-live) w bazie danych,
  - inny równoważny sposób akceptowalny przez prowadzącego.

#### Wymagane praktyki inżynieryjne

- **Architektura oparta na Use Case’ach**:  
  Kod powinien być podzielony na Use Case’y zgodnie z zasadami czystej architektury lub innej architektury zaakceptowanej przez prowadzącego. Oznacza to w szczególności:
  - wydzielenie warstwy aplikacyjnej od warstwy infrastruktury,
  - brak bezpośrednich odwołań do technologii infrastrukturalnych (HTTP, ORM) w logice aplikacyjnej.  
  Minimalnie oczekuje się co najmniej trójwarstwowej struktury aplikacji: prezentacja/API, logika aplikacyjna, infrastruktura.

- **Integracja z serwisem produktowym**:  
  Dodanie produktu do koszyka polega na przekazaniu identyfikatora produktu z serwisu produktowego. Pozostałe informacje o produkcie (nazwa, opis, aktualna cena) są pobierane z serwisu produktowego poprzez wywołania HTTP (np. REST). System koszyka **nie** powinien bezpośrednio odczytywać bazy danych serwisu produktowego.

---

### Ocena 4.0

Aby uzyskać ocenę 4.0, rozwiązanie musi spełniać wymagania na ocenę 3.5 oraz dodatkowo zapewniać poniższe atrybuty jakościowe.

#### Atrybuty jakościowe

##### Współbieżność 

Wielu użytkowników musi mieć możliwość równoczesnego zarządzania własnymi koszykami bez konfliktów lub przecieków danych. Oznacza to, że:

- Operacje wykonywane przez jednego użytkownika nie mogą wpływać na koszyki innych użytkowników.
- Dane pojedynczego koszyka nie powinny być nadpisywane w nieprzewidywalny sposób przy równoczesnych modyfikacjach (np. dwie operacje dodania produktu wykonywane w tym samym czasie nie mogą powodować utraty którejś z operacji). 

##### Spójność operacyjna 

W tym samym czasie użytkownik może dodawać i usuwać produkty z koszyka na różnych urządzeniach (np. telefon, laptop). System musi zapewniać, że żadna z tych operacji nie zostanie utracona ani pominięta. W praktyce oznacza to, że:

- Sekwencja operacji na koszyku jest przetwarzana w sposób deterministyczny (np. poprzez wersjonowanie, zdarzenia domenowe).
- Odczyt koszyka po serii operacji odzwierciedla wszystkie wykonane operacje w prawidłowej kolejności.  

###### Uwaga 

Tworzenie i zamykanie koszyka może być realizowane w tym samym czasie tylko na jednym urządzeniu danego użytkownika, a więc nie jest wymagane równoległe przetwarzanie zdarzeń dla tych operacji. Należy jednak zapewnić, że nie da się np. dwukrotnie zatwierdzić tego samego koszyka.

---

### Ocena 4.5

Aby uzyskać ocenę 4.5, rozwiązanie musi spełniać wymagania na ocenę 4.0 oraz dodatkowo spełniać poniższe wymagania.

#### Wymagania funkcjonalne

- **Powiadomienie o rozpoczęciu realizacji zamówienia – integracja z Lab 2**:  
  Powiadomienie musi być wysłane do systemu notyfikacji opracowanego w Laboratorium 2. Integracja może być zrealizowana np. poprzez:
  - wywołanie HTTP API systemu notyfikacji,
  - wysłanie komunikatu do kolejki wiadomości, jeżeli taki mechanizm został wykorzystany w Laboratorium 2. 

#### Wymagane praktyki inżynieryjne

- **Niezależne moduły koszyka i zamówień**:  
  Moduł odpowiedzialny za obsługę koszyka oraz moduł odpowiedzialny za obsługę zamówień muszą być wyraźnie oddzielone w kodzie. Oznacza to m.in.:
  - brak bezpośredniego dostępu modułu koszyka do tabel bazy danych modułu zamówień (i odwrotnie),
  - komunikację pomiędzy modułami poprzez jasno zdefiniowane interfejsy (np. komendy, zdarzenia domenowe, API),
  - możliwość zastąpienia jednego modułu inną implementacją bez konieczności modyfikowania drugiego.

- **Integracja z systemem notyfikacji**:  
  Powiadomienia muszą być wysyłane w sposób niezawodny, tzn. awaria systemu notyfikacji nie może powodować utraty informacji o rozpoczęciu realizacji zamówienia. Można zastosować m.in.:
  - mechanizm ponawiania wysyłki (retry),
  - kolejkę pośredniczącą,
  - zapisywanie informacji o nieudanych próbach wysłania powiadomienia i późniejsze ponawianie.  


#### Atrybuty jakościowe

##### Spójność danych produktowych

Wymagane jest uwzględnienie faktu, że w trakcie wykonywania operacji cena produktu może ulec zmianie. Mechanizmy przeliczania wartości koszyka muszą:

- brać pod uwagę aktualne ceny produktów pobierane z serwisu produktowego,
- zapewniać przewidywalne, jasno zdefiniowane zachowanie systemu w przypadku rozbieżności pomiędzy ceną z momentu dodania produktu do koszyka a jego aktualną ceną.  

---

### Ocena 5.0

Aby uzyskać ocenę 5.0, rozwiązanie musi spełniać wymagania na ocenę 4.5 oraz dodatkowo spełniać poniższe wymagania funkcjonalne i architektoniczne.

#### Wymagania funkcjonalne i architektoniczne

- **Oddzielne bazy danych dla zapisów i odczytów**:  
  System musi wykorzystywać co najmniej dwie różne bazy danych lub dwa niezależne schematy:
  - bazę zapisową (write model), w której wykonywane są operacje modyfikujące stan (komendy),
  - bazę odczytową (read model), zoptymalizowaną pod szybkie zapytania (np. widoki agregujące dane koszyka i zamówień).  
  Dane w bazie odczytowej nie muszą być aktualne w czasie rzeczywistym, ale powinny być spójne w rozsądnym czasie (eventual consistency).
- **Mechanizm projekcji danych**:  
  Musi istnieć mechanizm, który przenosi zmiany z modelu zapisu do modelu odczytu. Może to być:
  - proces nasłuchujący na zdarzenia domenowe i aktualizujący bazę odczytową,
  - mechanizm synchronizacji uruchamiany cyklicznie,
  - inny równoważny sposób, jasno udokumentowany.  
  
---

## Przydatne linki

- https://bulldogjob.pl/readme/cqrs-i-event-sourcing-czyli-latwa-droga-do-skalowalnosci-naszych-systemow
- https://www.youtube.com/watch?v=yFjzGRb8NOk&ab_channel=DrawingBoxes
- https://www.youtube.com/watch?v=n_o-xuuVtmw&ab_channel=NickChapsas
- https://www.youtube.com/watch?v=RUw7Gajn6Vs&ab_channel=NickChapsas
- https://www.youtube.com/watch?v=jU5aKVQmBeM&ab_channel=bITconf
- https://github.com/oskardudycz/EventSourcing.NetCore/pull/174/files#diff-01a91e97c1138ce92b2aae77b0b669bae98b8f6c0e3b902b95e334658a782248
- https://github.com/kartik786-git/CleanArchWithCQRSandMediator
- https://refactoring.guru/pl/design-patterns/mediator
- https://github.com/MrEshboboyev/concurrency-control