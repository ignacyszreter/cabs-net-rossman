# Cabs - system przewozów taksówkarskich


Cabs to system firmy świadczącej usługi przewozowe. Klient zamawia przejazd z adresu na adres, system go wycenia i szuka kierowcy w okolicy. Następnie po dokonaniu przejazdu wystawia fakturę. We flocie znajdują się auta różnych klas, honoraria kierowców, program lojalnościowy oferujący bonusy za przejechane kilometry czy umowy z partnerami. 


## Aktorzy 

| Aktor | Operacje | Charakterystyka |
|---|---|---|
| **Klient** | zamówienie przejazdu, opłacenie przejazdu, zgłoszenie roszczenia | osoba albo firma; status VIP albo zwykły; rozlicza się w systemie pre-paid, po przejeździe lub miesięcznie |
| **Kierowca** | przyjęcie przejazdu, wykonanie przejazdu | statusy: zgłoszony do pracy, przyjęty do pracy, aktywny, nieaktywny. Ma prawo jazdy, stawkę za którą pracuje. Opisywany jest takimi atrybutami jak punkty karne, lata doświadczenia, ważność badań czy narodowość |
| **Biuro** | rejestracja kierowcy, rejestracja auta, obsługa umowy, rozstrzyganie roszczeń |  |

## Cykl życia przejazdu

| Krok | Opis | Stan w kodzie |
|---|---|---|
| **Zamówienie** | Klient podaje adres odbioru, adres docelowy i klasę auta. System wylicza dystans przejazdu i wycenia przejazd | `Draft` |
| **Publikacja** | Zamówienie propagowane jest do kierowców. System proponuje przejazd kolejnym aktywnym kierowcom na zmianie, którzy znajdują się najbliżej klienta | `WaitingForDriverAssignment` |
| **Przyjęcie** | Kierowca odrzuca lub przyjmuje przejazd. Następnie jedzie po klienta. W przypadku nieprzyjęcia zamówienia przez żadnego z kierowców zamówienie kończy się niepowodzeniem. | `TransitToPassenger`, `DriverAssignmentFailed` |
| **Start** | Kierowca zabiera klienta | `InTransit` |
| **Zakończenie** | Kierowca dojeżdża do adresu końcowego. Obliczana jest cena końcowa, honorarium kierowcy, bonusowe mile dla klienta i wystawiana jest faktura | `Completed` |
| **Anulowanie** | Klient rezygnuje przed startem. Kierowca dostaje powiadomienie. | `Cancelled` |

Dopóki kierowca nie wystartował, klient może zmienić adres odbioru albo adres
docelowy. Zmiana adresu skutkuje przeliczeniem dystansu taryfy i jej ceny.

## Reszta systemu

- **Flota i klasy auta** — Eco, Regular, Van i Premium. Klasę można wybrać przy zamówieniu, jeśli we flocie jest akurat odpowiedni samochód.
- **Zmiana kierowcy** — kierowca loguje się na zmianę podając konkretny samochód (marka, numer rejestracyjny, klasa) i wylogowuje po zmianie. Ofertę przejazdu otrzymuje tylko aktywny kierowca z autem pasującym do żądanej klasy. 
- **Pozycja kierowcy** — auto na zmianie raportuje pozycję GPS. Dane te służą do obliczenia odległości do klienta zlecającego przejazd i wyliczenia długości przejechanego dystansu. 
- **Honorarium kierowcy** — zarobki kierowcy. Obliczane są na bazie ceny
  przejazdu i stawki kierowcy; stawka ma typ, wysokość i kwotę minimalną. Z
  wynagrodzeń za przejazdy generowany jest raport miesięczny i roczny dla kierowcy.
- **Roszczenie** — klient zgłasza roszczenie do przejazdu: numer i opis
  zdarzenia. Część roszczeń system rozstrzyga sam, na podstawie historii
  klienta, jego statusu i ceny przejazdu; resztę eskaluje do biura. Klient
  dostaje powiadomienie o zwrocie, kierowca prośbę o szczegóły.
- **Mile** — klient zapisuje się do programu i dostaje mile za przejazdy. Mile
  mają datę ważności, biuro może dopisać mile specjalne, a klient przenieść je
  do innego klienta. Kolejność wydawania bonusowych mil zależy od statusu klienta i jego
  historii.
- **Umowa z partnerem** — numer, przedmiot i załączniki. Strony proponują
  załączniki, następnie je akceptują lub odrzucają. Umowa może być negocjowana, przyjęta albo odrzucona.
- **Faktura** — powstaje po zakończonym przejeździe, na kwotę i nazwę klienta.
- **Raport kierowcy** — dane kierowcy (narodowość, lata doświadczenia, punkty karne), ostatnie przejazdy etc.


