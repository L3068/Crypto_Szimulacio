# Crypto Szimuláció

Ez a projekt egy kriptovaluta szimulációs REST API C# és ASP.NET Core technológiákkal, mely lehetővé teszi a felhasználók, pénztárcák és kriptovaluta-tranzakciók kezelését.

## Funkciók

- REST API végpontok:
  - Felhasználó regisztráció / bejelentkezés
  - Pénztárca létrehozás
  - Kriptovaluta vásárlás, eladás
  - Tranzakciók lekérdezése
- Adatbázis-kezelés: Entity Framework Core
- Backend: ASP.NET Core Web API

## Használt technológiák

- ASP.NET Core Web API
- Entity Framework Core
- LINQ
- SQL Server
- Swagger 

## Futtatás
 
1. Állitsd be a megfelelő adatbázis elérési utat a Program.cs fájlban:
   ```bash
   builder.Services.AddDbContext<AppDbContext>(options =>
    {
    options.UseSqlServer("Server=.\\SQLEXPRESS;Database=CryptoDb;Trusted_Connection=True;TrustServerCertificate=True;");
    });

2. Tools > NuGet Package Manager > Package Manager Console:
   ```bash
   add-migration *migráció neve*
   update-database

3. A létrehozott adatbázist töltsd fel a cryptoinitdbdata.sql tartalmával

4. Futtasd az alkalmazást
