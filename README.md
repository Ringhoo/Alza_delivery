# Alza Delivery API

.NET CORE ASP API aplikace řešící plánování rozvozu balíků z alza skladů do alzaboxů

## Spuštění
Defaultně je vytvoření docker kontainer pro databázi a aplikace v development prostředí.

Je tam i target "final", ale defaultně se pouští development s bind svazkem.

### Příkazy pro spuštění
- docker compose up --build -d
- docker compose exec app dotnet test

#### Pouze pro první spuštění - migrace a  seedování testovacích dat
- docker compose exec app dotnet ef database update
- docker compose exec app dotnet run -- seed

### Client
Existuje soubor "Alza_delivery.http", který lze použít pro volání API bez nutnosti Postmana nebo jiného klienta.

## Poznámky
Db secrets jsem nechal ve stringu - nemělo by být

Testy se pouštějí lokálně, ideálně by asi bylo vhodné mít pro ně stage v dockerfilu, ale pro tyto účely nechávám jen takto.

Aplikace je navržena tak, aby se pro každý sklad spouštěli výpočty na samostatném vlákně.

- Defaultně se EP pouští pro všechny sklady v databázi, ale v requestu lze specifikovat pole `WarehouseIds` pro omezení
- Zároveň request obsahuje	`PlanningDate` pro specifikaci kterých balíčků se plánování týká, novější nebudou brány v potaz
  - POZOR: Zpracované balíčky se nijak neoznačují, tzn. plánování se dá volat pořád do kola pro všechny balíčky do daného data.
	- Reálně by se balíčky označovaly nějakým stavem, aby se do dalšího plánovaní už nezahrnovali.

## Předpoklady
- Dle instrukcí držím objem v m3, váhu v kg, ale definoval jsem nejmenší jednotky jako gramy a mililitry (cm3).
- Uvažuji identická vozidla, resp. že všechny vozidla mají stejné limity dle zadání.

## Pochopení problému

V první řadě, můj algoritmus není optimální z hlediska profitu. Takový algoritmus by musel projít obrovské množství kombinací. Už počet všech podmnožin je 2^pocet_baliku. Což je obrovské množství, a pro řádově **statisíce balíků** nepoužitelné.

### Jak jsem postupoval

Originální představa byla, že bych si udělal seznam front, kde každá fronta obsahuje pouze balíčky s daným objemem. 

Např:        1m3   |    2m3    |    3m3    |    4m3

Každá by byla seřazena podle výnosnosti. Brala by se vždy nejvýnosnější a ta se přiřazovala. 

Problém s tím je, že by se mohly jako první zaplnit místa největšími balíčky, i když třeba 2 menší do stejného objemu by vynesly víc.

Dalo by se to vyřešit, že by se např. pro volné 3m3 zkoumaly nejvýhodnější kombinace (1-1-1, 1-2, 3)

To už by bylo přesnější, ale je zde stále problém, že není vůbec zohledněná váha a musela by se řešit nějaká logika, když kombinace bude přes limit.

A i kdyby to fungovalo, tak vzhledem k předpokladu objemy balíčků diskretizovat/zaokrouhlovat na nejbližší povolenou hodnotu, by se volné místo zbytečně považovalo za zaplněné i kdyby to tak reálně nebylo. Bez diskretizace by se ztratila výhoda menšího množství front a počet kombinací by rychle rostl.

Takže tohle byla pěkná představa, ale slepá ulička. 

Snažil jsem se třídit balíčky nějakou externí hierarchií a seskupováním, ale co kdyby každý balíček měl potřebné informace sám v sobě.

### Zvolený algoritmus

Víme, že nás zajímá profitabilita, ale zároveň jsme omezení objemem, ale i váhou. 

Na základě těchhle požadavků můžeme spočítat pomocnou metriku, podle které jednotlivé balíčky hodnotit. Když vydělíme profitabilitu poměrem náročnosti objemu a váhy:

**priority** = profitability / ((objem_balík / objem_auto) + (váha_balík / váha_auto))

tak dostáváme nějakou metriku, která nám dá jak hodnotný ten balík je vzhledem k jeho objemu a váze. 

Metrika je v algoritmu ještě upravná váhami - podle toho jaké jsou v aktuálním oběhu nároky balíčků na váhu a objem. Vzácnější zdroj je penalizován.

Na základě této metriky můžeme balíčky seřadit a přiřazovat do nejvhodnější dodávky. A to tak že u každého balíku projdu dodávky, do kterých se ještě vejde, a vyberu tu, ve které po vložení zůstane nejlépe využitelná kapacita.
Tzn. nejplnější auto, kam se balíček ještě vejde s přihlédnutím na vzácnost zdroje = prioritizací zaplnění zdroje s větší váhou.

Jak jsem zmínil, řešení není optimální, protože neřeší nejlepší možné kombinace, místo toho se rozhoduje který balík vzít a kam ho dát v reálném čase podle námi nastavených kritérií. Vzhledem k jeho časové složitosti by to mohl být dostatečný kompromis.

Složitost by měla být pro pocet_baliku = n, pocet_aut = m

 **O(n*log n + n*m)**

a škálovat mnohem lépe než procházení kombinací balíčků.