# Workshop Manager 2.0

System zarządzania warsztatem samochodowym zbudowany w ASP.NET Core 9 z wykorzystaniem Entity Framework Core, ASP.NET Identity oraz nowoczesnych narzędzi do mapowania, testowania i monitorowania wydajności.

## Spis treści

- [Autorzy](#autorzy)
- [Opis projektu](#opis-projektu)
- [Funkcjonalności](#funkcjonalności)
- [Technologie](#technologie)
- [Struktura projektu](#struktura-projektu)
- [Instalacja i uruchomienie](#instalacja-i-uruchomienie)
- [System logowania i role](#system-logowania-i-role)
- [CI/CD i testy wydajności](#cicd-i-testy-wydajności)
- [Dokumentacja API](#dokumentacja-api)

## Autorzy
Alicja Kowalska
Wiktoria Kosek

## Opis projektu

Workshop Manager to kompleksowy system do zarządzania warsztatem samochodowym, który umożliwia:

- **Zarządzanie klientami i pojazdami** - pełny CRUD z możliwością przechowywania zdjęć pojazdów
- **Obsługa zleceń serwisowych** - tworzenie zleceń, przypisywanie mechaników, zarządzanie statusami
- **Katalog części i czynności** - zarządzanie magazynem części oraz definiowanie czynności serwisowych
- **System komentarzy** - wewnętrzna komunikacja i historia przebiegu napraw
- **Raporty PDF** - generowanie raportów finansowych i technicznych
- **Monitoring wydajności** - indeksy bazodanowe, logowanie, testy obciążeniowe

## Funkcjonalności

### Zarządzanie klientami
- Tworzenie, edycja, usuwanie i wyszukiwanie klientów
- Przeglądanie historii pojazdów i zleceń dla każdego klienta
- Walidacja danych kontaktowych

### Zarządzanie pojazdami
- Rejestracja pojazdów z numerami VIN i rejestracyjnymi
- Upload i przechowywanie zdjęć pojazdów
- Powiązanie z właścicielami i historia napraw

### Zlecenia serwisowe
- Tworzenie zleceń z automatycznym przypisaniem numerów
- Statusy: Nowe, W trakcie, Zakończone, Anulowane
- Przypisywanie mechaników do konkretnych zadań
- Kalkulacja kosztów robocizny i części

### Czynności i części
- Katalog standardowych czynności serwisowych
- Zarządzanie magazynem części z cenami
- Automatyczne obliczanie kosztów napraw

### System komentarzy
- Wewnętrzne komentarze do zleceń
- Historia komunikacji zespołu
- Śledzenie postępu prac

### Raporty i analizy
- Raporty kosztów dla klientów, pojazdów i okresów
- Eksport do PDF z profesjonalnym formatowaniem
- Automatyczne raporty e-mail (BackgroundService)

## Technologie

### Backend
- **ASP.NET Core 9** - framework aplikacji webowej
- **Entity Framework Core** - ORM z podejściem Code First
- **SQL Server** - baza danych
- **ASP.NET Identity** - uwierzytelnianie i autoryzacja
- **Mapperly** - mapowanie obiektów DTO <-> Entity
- **NLog** - logowanie błędów i zdarzeń

### Frontend
- **Razor Pages** - widoki z Bootstrap 5
- **Bootstrap 5** - responsywny interfejs użytkownika
- **Font Awesome** - ikony
- **JavaScript/jQuery** - interaktywność

### Testy i monitoring
- **NBomber** - testy wydajności i obciążeniowe
- **SQL Server Profiler** - analiza zapytań
- **Swagger/OpenAPI** - dokumentacja API

### CI/CD
- **GitHub Actions** - automatyczne budowanie i testowanie

## Struktura projektu

```
WorkshopManager/
├── Controllers/           # Kontrolery MVC/API
│   ├── AccountController.cs
│   ├── CustomersController.cs
│   ├── VehiclesController.cs
│   ├── ServiceOrdersController.cs
│   └── ...
├── Models/               # Modele domenowe (Entity)
│   ├── Customer.cs
│   ├── Vehicle.cs
│   ├── ServiceOrder.cs
│   └── ...
├── DTOs/                 # Obiekty transferu danych
├── Services/             # Logika biznesowa
│   ├── Interfaces/
│   ├── CustomerService.cs
│   ├── VehicleService.cs
│   └── ...
├── Mappers/              # Mappery Mapperly
│   ├── CustomerMapper.cs
│   ├── VehicleMapper.cs
│   └── ...
├── Data/                 # Kontekst bazy danych
│   ├── ApplicationDbContext.cs
│   ├── DbInitializer.cs
│   └── Migrations/
├── Views/                # Widoki Razor
│   ├── Customers/
│   ├── Vehicles/
│   ├── ServiceOrders/
│   └── ...
├── BackgroundServices/   # Usługi w tle
├── PerformanceTests/     # Testy NBomber
├── Reports/              # Generowane raporty PDF
├── wwwroot/             # Zasoby statyczne
│   ├── uploads/         # Zdjęcia pojazdów
│   ├── css/
│   └── js/
└── logs/                # Logi NLog
```

## Instalacja i uruchomienie

### Wymagania
- .NET 9 SDK
- SQL Server (LocalDB wystarczy)
- Visual Studio 2022 lub VS Code

### Kroki instalacji

1. **Klonowanie repozytorium**
```bash
git clone https://github.com/loczus887/Warsztat-samochodowy-.NET
cd workshop-manager
```

2. **Konfiguracja bazy danych**
```bash
# Uruchomienie migracji
dotnet ef database update

# Alternatywnie - automatyczna inicjalizacja przy pierwszym uruchomieniu
```

3. **Konfiguracja appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WorkshopManagerDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

4. **Uruchomienie aplikacji**
```bash
dotnet run
```

Aplikacja będzie dostępna pod adresem: `https://localhost:7152`

## System logowania i role

### Uwierzytelnianie
Aplikacja wykorzystuje **ASP.NET Identity** do zarządzania użytkownikami i uwierzytelnianiem:

- Rejestracja nowych użytkowników
- Logowanie z zapamiętywaniem sesji
- Bezpieczne przechowywanie haseł (hash + salt)
- Walidacja siły hasła

### Role użytkowników

#### Admin
**Pełne uprawnienia do zarządzania systemem:**
- Zarządzanie użytkownikami i przypisywanie ról
- Zarządzanie katalogiem części
- Dostęp do wszystkich raportów i statystyk
- Konfiguracja systemu

**Domyślne konto:**
- Email: `admin@workshop.com`
- Hasło: `Admin123!`

#### Mechanik (Mechanic)
**Uprawnienia związane z pracą techniczną:**
- Przeglądanie przypisanych zleceń
- Aktualizacja statusu prac
- Dodawanie komentarzy do zleceń
- Rejestrowanie użytych części i czasu pracy
- Dostęp tylko do własnych zleceń

**Domyślne konto:**
- Email: `mechanic@workshop.com`
- Hasło: `Mechanic123!`

#### Recepcjonista (Receptionist)
**Uprawnienia do obsługi klienta:**
- Zarządzanie klientami (CRUD)
- Zarządzanie pojazdami klientów
- Tworzenie nowych zleceń serwisowych
- Przypisywanie mechaników do zleceń
- Generowanie raportów dla klientów
- Zarządzanie katalogiem części

**Domyślne konto:**
- Email: `receptionist@workshop.com`
- Hasło: `Reception123!`

### Autoryzacja

Aplikacja wykorzystuje autoryzację opartą na rolach:

```csharp
[Authorize(Roles = "Admin")]                    // Tylko Admin
[Authorize(Roles = "Admin,Receptionist")]       // Admin lub Recepcjonista
[Authorize(Roles = "Mechanic")]                 // Tylko Mechanik
```

## CI/CD i testy wydajności

### GitHub Actions Workflow

Plik `.github/workflows/dotnet-ci.yml` zawiera konfigurację CI/CD:

```yaml
name: .NET CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Test
      run: dotnet test --no-build --verbosity normal
      
    - name: Build Docker image (optional)
      run: docker build -t workshop-manager .
```

### Testy wydajności NBomber

Plik `PerformanceTests/OrdersLoadTest.cs` zawiera testy obciążeniowe:

- **Endpoint testowany:** `GET /api/serviceorders/active`
- **Konfiguracja:** 50 równoległych użytkowników, 100 żądań
- **Raport:** Automatycznie generowany w `Reports/nbomber-report.pdf`

### Optymalizacja wydajności

#### Indeksy bazodanowe
Dodane indeksy nieklastrowane dla często używanych zapytań:

```csharp
// Przykłady zoptymalizowanych zapytań
modelBuilder.Entity<Customer>()
    .HasIndex(c => c.PhoneNumber)
    .HasDatabaseName("IX_Customers_PhoneNumber");

modelBuilder.Entity<ServiceOrder>()
    .HasIndex(o => new { o.Status, o.CreatedAt })
    .HasDatabaseName("IX_ServiceOrders_Status_CreatedAt");
```

**Dokumentacja wydajności:** `Reports/raport-indeksy.pdf`

#### SQL Profiler
Monitoring zapytań SQL w czasie rzeczywistym:
- **Dokumentacja:** `Reports/raport-sql-profiler.pdf`
- **Endpoint monitorowany:** `/api/serviceorders`

## Logowanie

### NLog Configuration

Aplikacja wykorzystuje **NLog** do kompleksowego logowania:

```xml
<!-- nlog.config -->
<nlog>
  <targets>
    <target name="fileTarget" 
            type="File" 
            fileName="logs/workshop-manager-${shortdate}.log"
            layout="${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}" />
  </targets>
  
  <rules>
    <logger name="*" minlevel="Info" writeTo="fileTarget" />
  </rules>
</nlog>
```

### Typy logowanych zdarzeń:
- **Info:** Operacje CRUD, logowania użytkowników
- **Warning:** Walidacja danych, próby dostępu bez uprawnień  
- **Error:** Błędy aplikacji, problemy z bazą danych
- **Debug:** Szczegółowe informacje diagnostyczne (tylko w Development)

### BackgroundService - Raporty e-mail

Usługa `OpenOrderReportBackgroundService.cs`:
- Generuje codzienne raporty otwartych zleceń
- Automatyczny eksport do PDF
- Wysyłka e-mail do administratorów
- **Raport przykładowy:** `Reports/raport-otwarte-naprawy.pdf`

## Dokumentacja API

Swagger UI dostępne pod adresem: `/swagger`

### Główne endpointy:

#### Klienci
- `GET /api/customers` - Lista klientów
- `POST /api/customers` - Tworzenie klienta
- `GET /api/customers/{id}` - Szczegóły klienta
- `PUT /api/customers/{id}` - Aktualizacja klienta
- `DELETE /api/customers/{id}` - Usuwanie klienta

#### Pojazdy
- `GET /api/vehicles` - Lista pojazdów
- `POST /api/vehicles` - Rejestracja pojazdu
- `POST /api/vehicles/{id}/upload-image` - Upload zdjęcia

#### Zlecenia serwisowe
- `GET /api/serviceorders` - Lista zleceń
- `GET /api/serviceorders/active` - Aktywne zlecenia
- `POST /api/serviceorders` - Tworzenie zlecenia
- `PUT /api/serviceorders/{id}/status` - Zmiana statusu

#### Raporty
- `GET /api/reports/customer/{id}` - Raport klienta
- `GET /api/reports/vehicle/{id}` - Raport pojazdu
- `GET /api/reports/monthly/{year}/{month}` - Raport miesięczny

