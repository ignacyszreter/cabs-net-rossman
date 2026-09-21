# Cabs — ćwiczenie modelarskie


## Kontekst systemu

Cabs to system firmy taksówkarskiej. Klient zamawia przejazd z adresu na adres, system następnie go wycenia, szuka kierowcy w okolicy. Po przejeździe wykonywane jest rozliczenie. Firma posiada we flocie cztery klasy aut. 

---

## Wymagania biznesowe

---

### 1. Zamówienie przejazdu

Klient podaje adres odbioru, adres docelowy i klasę auta, którym chce jechać. 
System oblicza dystans i prezentuje **estymowaną cenę przed zamówieniem**. Estymowana cena pokazywana jest bez akceptacji kierowcy.

W momencie, kiedy klient zamówi przejazd, wysyłane jest żądanie do kierowców. System proponuje przejazd aktywnym kierowcom na zmianie - sekwencyjnie od tych, którzy są najbliżej klienta i poruszają się pojazdem pasującym do zamówionej klasy. Kierowca może ofertę przyjąć albo odrzucić. Gdy żaden kierowca nie przyjmie oferty, zamówienie kończy się niepowodzeniem.

Dopóki kierowca nie *wystartował* zamówienia, klient może zmienić adres odbioru/adres docelowy. Zmiana adresu powoduje przeliczenie dystansu i ceny. Klient może zmienić adres maksymalnie trzy razy. Warunkiem koniecznym zmiany adresu jest to, że nowy adres znajduje się w promieniu 250 metrów od adresu pierwotnego.


Cykl życia zamówienia i przejazdu:

| Krok | Opis |
|---|---|
| zapytanie o cenę | klient pyta o koszt, zanim zamówi przejazd |
| zamówienie | klient potwierdza adres odbioru/adres docelowy oraz klasę auta |
| publikacja | system proponuje przejazd kolejnym kierowcom |
| przyjęcie | kierowca przyjmuje ofertę, rozpoczyna kurs po klienta |
| start | kierowca zabiera klienta |
| zakończenie | kierowca dojeżdża, obliczana jest cena końcowa i honorarium dla kierowcy |
| anulowanie | klient rezygnuje przed startem, kierowca dostaje stosowne powiadomienie |

Klient widzi na ekranie dane przejazdu: adresy, cenę, nazwę taryfy, imię kierowcy, klasę auta, status i datę. Dokładnie te same dane widzi księgowość w zestawieniu miesięcznym.

---

### 2. Cennik

Cennik definiuje sposób liczenia ceny przejazdu. Cena to opłata bazowa plus stawka za kilometr, obie zależą od taryfy. O taryfie decyduje moment zamówienia.

| Taryfa | Kiedy obowiązuje | Stawka za km | Opłata bazowa |
|---|---|---|---|
| Standard | tydzień roboczy | 1,00 | 9 |
| Weekend | sobota i niedziela w dzień | 1,50 | 8 |
| Weekend+ | piątek i sobota od 17:00 do 6:00 | 2,50 | 10 |
| Sylwester | 31 grudnia i 1 stycznia do 6:00 | 3,50 | 11 |

Taryfy są ustalane przez dział finansów. Nowa taryfa obowiązuje od wskazanej daty i **nie wpływa na ceny przejazdów już wykonanych**. Dział finansów symuluje też ceny hipotetyczne, zadając pytania pokroju „ile kosztowałyby przejazdy z
ostatniego kwartału przy nowej stawce weekendowej?”.

Dodatkowo istnieje dopłata za oczekiwanie na pasażera i dopłata za
przewóz poza miastem.

---

### 3. Kierowcy

Kierowca jest rejestrowany przez biuro. Kierowca ma imię, nazwisko, numer prawa jazdy i statusy: zgłoszony do
pracy, przyjęty do pracy, aktywny, nieaktywny.

Kierowca posiada **zestaw atrybutów**: punkty karne, narodowość, lata doświadczenia, data ważności
badań lekarskich, uwagi z badań, adres e-mail, miejsce urodzenia, nazwa firmy, dla której pracuje. Biuro dodaje nowe typy atrybutów kilka razy w roku.

Kierowca ma też **zestaw usług, które świadczy**: przewóz osób, przewóz paczek, przewóz zwierząt, przewóz
jedzenia, język angielski, fotelik dziecięcy. Klient może zamówić przejazd wymagający konkretnej usługi: na przykład
przewóz psa albo kierowcę mówiącego po angielsku. Wtedy ofertę dostaje tylko kierowca, który świadczy tę usługę.

**Stawka kierowcy** jest negocjowana indywidualnie. Pracownik biura rozmawia z kierowcą, bierze pod uwagę doświadczenie, historyczną liczbę roszczeń czy punktualność. Na tej podstawie ocenia, ile kierowca jest wart dla firmy. Wynikiem negocjacji jest stawka: typ (kwotowa lub procentowa), wysokość i kwota minimalna. 

**Zmiana.** Kierowca loguje się na zmianę i podaje konkretne auto, którego używa: markę, numer rejestracyjny i
klasę. Po zmianie się wylogowuje. Ofertę przejazdu dostaje tylko kierowca zalogowany na zmianę.

**Pozycja.** Auto na zmianie raportuje pozycję GPS co kilka sekund. Pozycja służy do dwóch rzeczy:
do wyliczenia odległości kierowcy od klienta oraz wyliczenia długości przejechanego dystansu.

---

### 4. Flota

We flocie są auta czterech klas: Eco, Regular, Van i Premium. Auto ma markę, numer rejestracyjny,
klasę i datę rejestracji. Biuro dokonuje rejestracji auta, może zmienić jego klasę lub wycofać konkretny pojazd z użytku. Klient wybiera klasę auta przy zamówieniu, o ile jest aktualnie dostępne auto tej klasy. 

---

### 5. Zakończenie przejazdu i rozliczenie

Po zakończeniu przejazdu (dojechaniu do adresu docelowego) system liczy cenę końcową opartą na faktycznym dystansie i taryfie obowiązującej w chwili zamówienia. System w tym momencie aktualizuje honorarium kierowcy i wpis w historii przejazdów. 

**Honorarium kierowcy** wynika z ceny przejazdu i stawki kierowcy. Stawka kwotowa to cena minus
kwota. Stawka procentowa to po prostu procent od ceny. Wynik nigdy nie jest niższy niż kwota minimalna określona dla kierowcy.
---

### 6. Wymagania krytyczne

- Kierowca nie może mieć dwóch przejazdów naraz.
- Cena pokazana przy zamówieniu jest ceną, którą klient zapłaci. Zmiana adresu przelicza ją i klient
  musi zobaczyć nową cenę przed startem.
- Przejazd rozlicza się dokładnie raz - kierowca otrzymuje wypłatę raz.
- System obsługuje dużo równoczesnych zamówień i strumień pozycji GPS z całej floty.
- Uprawnienie do zamawiania w cudzym imieniu jest sprawdzane w każdym punkcie systemu.
- Historia przejazdów z podziałem na sposoby zapłaty jest zawsze dostępna.
- Auto w warsztacie i kierowca bez ważnych badań nie dostają ofert przejazdu.

---

