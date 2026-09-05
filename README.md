# Crypto Szimuláció

Kriptovaluta-kereskedési szimuláció REST API, ASP.NET Core 9 és Entity Framework Core alapon.
A felhasználók virtuális egyenleggel kereskednek: vásárolhatnak, eladhatnak, válthatnak
kriptovaluták között, és követhetik a portfóliójuk nyereségét. Az árakat egy háttérszolgáltatás
mozgatja véletlenszerűen.

## Tartalom

- [Technológiák](#technológiák)
- [Projektstruktúra](#projektstruktúra)
- [Előfeltételek](#előfeltételek)
- [Indítás](#indítás)
- [Konfiguráció](#konfiguráció)
- [Authentikáció](#authentikáció)
- [Végpontok](#végpontok)
- [Tesztek](#tesztek)

## Technológiák

- ASP.NET Core 9 Web API
- Entity Framework Core 9 (SQL Server)
- JWT bearer authentikáció, ASP.NET Core Identity jelszó-hasheléssel
- Swagger / OpenAPI
- xUnit (unit- és integrációs tesztek)

## Projektstruktúra

| Projekt | Tartalom |
| --- | --- |
| `Crypto_Simulation` | Web API: controllerek, DI-konfiguráció, hibakezelés |
| `Crypto_Simulation.DataContext` | Entitások, DTO-k, `AppDbContext`, migrációk, kivételtípusok |
| `Crypto_Simulation.Services` | Üzleti logika: kereskedés, pénztárca, profit, árfrissítés |
| `Crypto_Simulation.Tests` | xUnit tesztek |

## Előfeltételek

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (SQL Server Express, LocalDB vagy Docker konténer is megfelel)
- `dotnet-ef` CLI a migrációkhoz:

  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Indítás

1. **Connection string beállítása.** Fejlesztéshez az `appsettings.Development.json` fájlban,
   vagy user-secrets segítségével:

   ```bash
   dotnet user-secrets --project Crypto_Simulation set \
     "ConnectionStrings:DatabaseConnection" \
     "Server=.\\SQLEXPRESS;Database=CryptoDb;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

2. **Adatbázis létrehozása:**

   ```bash
   dotnet ef database update \
     --project Crypto_Simulation.DataContext \
     --startup-project Crypto_Simulation
   ```

3. **Demo adatok betöltése** (opcionális) — futtasd a
   `Crypto_Simulation/cryptoinitdbdata.sql` fájlt a létrehozott adatbázison.
   A demo felhasználók jelszava `Demo1234!`; az `alice` fiók adminisztrátor.

4. **Alkalmazás indítása:**

   ```bash
   dotnet run --project Crypto_Simulation
   ```

   A Swagger UI fejlesztői módban a `/swagger` útvonalon érhető el.

## Konfiguráció

| Kulcs | Leírás | Alapérték |
| --- | --- | --- |
| `ConnectionStrings:DatabaseConnection` | SQL Server connection string (kötelező) | – |
| `Jwt:Key` | Szimmetrikus aláíró kulcs, legalább 32 karakter | Development-ben generált |
| `Jwt:Issuer` / `Jwt:Audience` | Token kibocsátó és célközönség | `Crypto_Simulation` |
| `Jwt:ExpiryMinutes` | Token élettartama percben | `60` |
| `Simulation:StartingBalance` | Új fiók kezdő egyenlege | `10000` |
| `Simulation:PriceUpdateIntervalSeconds` | Árfrissítés gyakorisága | `30` |
| `Simulation:MaxPriceFluctuation` | Maximális relatív árelmozdulás tickenként | `0.03` |
| `Simulation:PriceHistoryRetentionDays` | Ennél régebbi árelőzmények törlődnek (0 = megtartás) | `30` |
| `Cors:AllowedOrigins` | Engedélyezett frontend originök tömbje | üres |

> **A repo szándékosan nem tartalmaz JWT aláíró kulcsot.** Fejlesztői módban az alkalmazás
> induláskor generál magának egy véletlenszerűt, így egy friss klón mindenféle beállítás nélkül
> elindul. Cserébe minden újraindítás után újra be kell jelentkezned, mert a korábban kiadott
> tokenek érvénytelenné válnak. Ha ezt el akarod kerülni, adj meg egy sajátot:
>
> ```bash
> dotnet user-secrets --project Crypto_Simulation set "Jwt:Key" "<legalább 32 karakter>"
> ```
>
> Minden más környezetben a kulcs megadása kötelező — `Jwt__Key` környezeti változóval,
> user-secrets-szel vagy key vaulttal. Az alkalmazás indításkor elszáll, ha hiányzik vagy
> 32 karakternél rövidebb.

## Authentikáció

A regisztráción, a bejelentkezésen és a piaci adatok olvasásán kívül minden végpont
bearer tokent igényel:

```bash
# 1. Regisztráció
curl -X POST http://localhost:5221/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"trader","email":"trader@example.com","password":"correct horse battery"}'

# 2. Bejelentkezés — a válasz tartalmazza a tokent
curl -X POST http://localhost:5221/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"trader@example.com","password":"correct horse battery"}'

# 3. Hívás tokennel
curl http://localhost:5221/api/trade/portfolio \
  -H "Authorization: Bearer <token>"
```

Két szerepkör létezik: `User` és `Admin`. Egy felhasználó csak a saját adatait éri el; admin
bármelyikét. Admin jogot adni közvetlenül az adatbázisban lehet:

```sql
UPDATE Users SET Role = 'Admin' WHERE Id = 1;
```

## Végpontok

A kereskedési műveleteknél a felhasználó azonosítója **mindig a tokenből** származik, nem a
kérés törzséből.

### Users — `/api/users`

| Metódus | Útvonal | Jogosultság |
| --- | --- | --- |
| `POST` | `/register` | nyilvános |
| `POST` | `/login` | nyilvános |
| `GET` | `/me` | bejelentkezett |
| `GET` | `/{userId}` | tulajdonos vagy admin |
| `PUT` | `/{userId}` | tulajdonos vagy admin |
| `DELETE` | `/{userId}` | tulajdonos vagy admin |

### Cryptos — `/api/cryptos`

| Metódus | Útvonal | Jogosultság |
| --- | --- | --- |
| `GET` | `/` | nyilvános |
| `GET` | `/{cryptoId}` | nyilvános |
| `GET` | `/price/history/{cryptoId}?fromUtc=&toUtc=&limit=` | nyilvános |
| `POST` | `/` | admin |
| `PUT` | `/price` | admin |
| `DELETE` | `/{cryptoId}` | admin |

### Trade — `/api/trade`

| Metódus | Útvonal | Jogosultság |
| --- | --- | --- |
| `POST` | `/buy` | bejelentkezett |
| `POST` | `/sell` | bejelentkezett |
| `POST` | `/convert` | bejelentkezett |
| `GET` | `/portfolio` | bejelentkezett |
| `GET` | `/portfolio/{userId}` | tulajdonos vagy admin |

### Wallet — `/api/wallet`

| Metódus | Útvonal | Jogosultság |
| --- | --- | --- |
| `GET` | `/{userId}` | tulajdonos vagy admin |
| `PUT` | `/{userId}` | admin |
| `DELETE` | `/{userId}` | admin |

Az egyenleg felülírása szándékosan admin művelet: ha a felhasználók maguknak állíthatnának
egyenleget, a nyereségszámítás értelmét vesztené.

### Transactions — `/api/transactions`

| Metódus | Útvonal | Jogosultság |
| --- | --- | --- |
| `GET` | `/{userId}?skip=&take=` | tulajdonos vagy admin |
| `GET` | `/details/{transactionId}` | tulajdonos vagy admin |

### Profit — `/api/profit`

| Metódus | Útvonal | Jogosultság |
| --- | --- | --- |
| `GET` | `/{userId}` | tulajdonos vagy admin |
| `GET` | `/details/{userId}` | tulajdonos vagy admin |

A hibaválaszok RFC 7807 `application/problem+json` formátumúak.

## Tesztek

```bash
dotnet test Crypto_Simulation.sln
```

A szvit unit teszteket (kereskedési matematika, átlagár-számítás, jelszókezelés, EF
modellkonfiguráció) és integrációs teszteket (a valódi HTTP pipeline authentikációval és
jogosultságkezeléssel, in-memory adatbázison) is tartalmaz. Adatbázis nem szükséges hozzájuk.

## Licenc

MIT — lásd a [LICENSE](LICENSE) fájlt.
