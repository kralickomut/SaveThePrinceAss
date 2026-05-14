# Nápad na hru — Save The Princess

## Koncept

2D side-scrollingová akční hra zasazená do středověkého hradu.
Hráč se ujme role Prince, který se vrátí domů a zjistí, že jeho otec — Král — se stal tyranem, utlačuje království a uvěznil Princeznu.
Ozbrojen pouze odhodláním (a zbraněmi, které po cestě najde), Princ bojuje přes patra hradu, aby zachránil svou sestru a odhalil pravdu o své rodině.

## Příběh

Král přišel o rozum. Vybírá daně tak, že lid upadá do chudoby, hází odpůrce do žaláře a drží Princeznu zamčenou.
Sloužící — kuchaři, pokojské, uklízeči — zanechávali po hradu skryté svitky dokumentující, co se děje. Tyto svitky jsou hráčovým oknem do příběhu.

Princ musí vystoupat hradem, číst svitky a skládat dohromady, co se stalo — a přitom probojovat cestu přes Královy stráže a výsledky jeho šílených alchymistických experimentů.

## Hratelnost

- **Žánr:** 2D side-scrollingový slasher / akční plošinovka
- **Engine:** Godot (C#)
- **Odhadovaná délka:** ~30–45 minut

### Základní smyčka

1. Vstup do patra hradu
2. Boj se strážemi a nepřáteli
3. Průzkum a hledání skrytých svitků (lore, nápovědy, humor)
4. Dosažení východu a přesun do dalšího patra

### Patra

| Patro | Místo              | Atmosféra                                                         |
| ----- | ------------------ | ----------------------------------------------------------------- |
| 1     | Vstup do hradu     | Světlé, úvodní — slabé stráže                                     |
| 2     | Obytné prostory    | Normální osvětlení — středně silní nepřátelé, nejvíce svitků zde  |
| 3     | Žalář / Vězení     | **Tma — hráč používá pochodeň**, záchrana Princezny              |

### Nepřátelé

Tři typy nepřátel s rostoucí obtížností:

- **Sliz** — Královy nezdařené alchymistické experimenty, uprchlé z laboratorní části žaláře. Pomalý, slabý, málo HP. Vyskytuje se na patrech 1–2.
- **Stráž** — základní hlídka, útok mečem, střední HP. Vyskytuje se na patrech 1–2.
- **Rytíř** — obrněný, pomalejší, ale silné údery, vysoké HP. Vyskytuje se na patrech 2–3.

### Zbraně

Zbraně se nacházejí po cestě — žádné obchody, žádné vylepšování:

- Pěsti (začátek)
- Krátký meč (patro 1)
- Široký meč (patro 2)
- _(volitelné)_ kouzelný meč — odměna na patře 3

### Klíčová mechanika — Svitky

Svitky jsou skryté v interaktivních dekoracích (regály s knihami, sudy, hrnce).
Slouží třem účelům:

- **Lore** — vysvětlují pozadí Králova šílenství
- **Nápovědy** — nenápadné tipy o nadcházejících nepřátelích nebo pastech
- **Humor** — každodenní stížnosti sloužících („Zase zelí k večeři")

### Mechanika pochodně (Patro 3)

Na patře žaláře je prostředí temné.
Hráč nese pochodeň, která vytváří omezený poloměr viditelnosti pomocí Godotova `PointLight2D`.
To mění pocit ze hry — pomalejší, napjatější, opatrnější.

## Vizuální styl

- **Pixel art** — assety ze [pixelrepo.com](https://pixelrepo.com) a podobných bezplatných knihoven pixel artu
- Pohled ze strany (standardní perspektiva 2D plošinovky)
- Sada dlaždic hradu s odlišným vzhledem pro každé patro (kamenný vchod → dřevěné pokoje → tmavý žalář)

<!-- ## Mimo rozsah (pro tuto verzi)

- Souboje s bossy
- Více než 3 typy nepřátel
- Systém inventáře nebo správy předmětů
- Systém dialogů (svitky jsou pouze textová okna ke čtení)
- Systém ukládání -->

## Časový plán

**Termín: 20.05**
