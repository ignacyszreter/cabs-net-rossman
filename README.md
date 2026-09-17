# Rozwój kodu

Kod będzie rozwijać się wraz z cotygodniową narracją szkoleniową. 
Zarówno pojawiać się w nim będą kolejne poprawki jak i odziedziczone po firmach partnerskich nowe moduły ;-) Jak to w prawdziwym legacy.

# Przeglądanie kodu

Poszczególne kroki refaktoryzacyjne najlepiej przeglądać używająć tagów. Każdy krok szkoleniowy, który opisany jest w odcinku Legacy Fighter posiada na końcu planszę z nazwą odpowiedniego taga. Porównać zmiany można robiąc diffa w stosunku do poprzedniego taga z narracji.

# Baza SQL Server do ćwiczeń z rozliczeniem kierowcy

Rozliczenie roczne kierowcy liczy procedura składowana `dbo.CalculateDriverMonthlyPayments`. Procedura działa tylko na SQL Server, więc testy rozliczenia potrzebują serwera. Stawiają go same, w Dockerze, przez Testcontainers. Pozostałe testy działają na SQLite w pamięci i serwera nie potrzebują.

## Przed pierwszym uruchomieniem testów

Potrzebujesz Docker Desktop albo innego silnika Dockera. Ściągnij obraz z wyprzedzeniem — waży około 1,5 GB:

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest
```

Na Macu z procesorem Apple obraz działa przez emulację `linux/amd64`. W Docker Desktop włącz *Use Rosetta for x86_64/amd64 emulation*.

## Uruchom testy

```bash
dotnet test src/CabsTests
```

Pierwszy test z kategorią `SqlServer` startuje kontener i czeka, aż serwer odpowie — około 20 sekund. Kontener zostaje po przebiegu, więc następne przebiegi biorą go gotowego. Każdy test tworzy własną bazę `CabsTests_<guid>`, zakłada schemat i procedury, a po teście bazę kasuje. Nie musisz niczego zakładać ręcznie.

Testy bez serwera — kontener wtedy nie wstaje i Docker nie jest potrzebny:

```bash
dotnet test src/CabsTests --filter "Category!=SqlServer"
```

Kontener zobaczysz na liście, a usuniesz go tak:

```bash
docker ps --filter label=org.testcontainers=true
docker rm -f <nazwa>
```

## Własny SQL Server zamiast Testcontainers

Testy biorą serwer ze zmiennej `CABS_SQLSERVER`, jeśli jest ustawiona. Kontener wtedy nie wstaje. Na przykład dla LocalDB:

```bash
export CABS_SQLSERVER="Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=True"
```

Użytkownik z connection stringa musi mieć prawo tworzyć i kasować bazy.

## Uruchom aplikację na SQL Server

Aplikacja czeka na serwer pod stałym adresem, więc do niej podnieś kontener z Compose:

```bash
docker compose up -d --wait
```

Polecenie startuje kontener `cabs-sqlserver` z SQL Server 2022 na porcie `1433`. Dane logowania są w `docker-compose.yml`: użytkownik `sa`, hasło `Cabs!Training1`.

Profil `CabsOnSqlServer` ustawia środowisko `Training`, a `appsettings.Training.json` wskazuje bazę `Cabs` w tym kontenerze:

```bash
dotnet run --project src/Cabs --launch-profile CabsOnSqlServer
```

Rozliczenie: `GET http://localhost:5286/drivers/{driverId}/settlements/{year}`. Kurs EUR przychodzi z API NBP, a święta z Nager.Date, więc aplikacja potrzebuje dostępu do internetu.

Kontener zatrzymasz przez `docker compose down`, a `docker compose down -v` skasuje też wolumen z danymi.
