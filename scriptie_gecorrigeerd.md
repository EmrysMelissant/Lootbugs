# Voorwoord
Als afsluiter van een bacheloropleiding is het gebruikelijk om een bachelorproject te doen. Omdat ik de in de opleiding Multimedia en Creatieve Technologieën de afstudeerrichting 3D en XR volg en zelf een fervente gamer ben, was het voor mij voor de hand liggend om als bachelorproject een 3D-spel te ontwikkelen. Dit spel kreeg de naam 'Lootbugs', omdat de speler een insect/spin is die buit moet verzamelen.
Ik wil hierbij graag mijn docenten, Koen Heylen en Pieter Jorissen, bedanken voor hun ondersteuning bij dit project.

<div style="page-break-after: always;"></div>
# Inhoudstafel
[TOC]

<div style="page-break-after: always;"></div>
# 1 Inleiding
Met dit bachelorproject wil ik een 3D-spel ontwikkelen. Dit geeft me de kans om al mijn aangeleerde vaardigheden en kennis om te zetten in een afgewerkt en speelbaar product. Tegelijkertijd ervaar ik wat het creëren van een spel in al zijn aspecten inhoudt.
Bij de uitwerking van het spel wil ik verder gaan dan wat we in de opleiding geleerd hebben en zo mijn profiel als game-developer verbreden.

<div style="page-break-after: always;"></div>
# 2 Onderdelen van de Scriptie

## 2.1 Projectomschrijving

### 2.1.1 Projectduiding
Ik koos ervoor om als project een 3D-game te ontwikkelen, genaamd 'Lootbugs'.
Lootbugs is een survival roguelike, waarin je solo of met vrienden een verlaten gebouw doorzoekt op items die je kan verzamelen en die jou iets opleveren. Tijdens het zoeken moet je oppassen voor de monsters die er ronddwalen. Telkens wanneer je terugkeert naar je basis, moet je de Queen voeden met je verzamelde loot. Als je haar tevreden stelt, kan je jezelf upgraden in de shop en opnieuw vertrekken, maar als ze niet tevreden is, word jij haar volgende snack. Het gaat er dus om om voldoende loot te verzamelen om zo te kunnen overleven en te stijgen in level.


### 2.1.2 Projectkeuze
Ik speel zelf heel veel online videogames, en ik merk dat ik altijd veel meer plezier heb als ik samen met vrienden speel. Het lag dus voor de hand dat, eens de keuze voor mijn bachelorproject op het ontwikkelen van een 3D-game was gevallen, ik voor het maken van een online multiplayer game zou kiezen. Dit is de eerste keer dat ik zoiets doe. Ik heb nog nooit eerder een online multiplayer game gemaakt, en ik wou als bachelorproject echt een uitdaging voor mijzelf kiezen.
Ik wou een simpel en makkelijk te spelen 3D-spel maken, dat spelers snel kunnen spelen en waarmee ze met vrienden heel veel lol kunnen maken. Het spel draait niet om eindeloze queestes, moeilijke opdrachten of sterke eindbosses, maar rond snel even doorheen het gebouw hollen, zoveel mogelijk loot verzamelen en vooral: het er levend vanaf brengen.
Met het ontwikkelen van dit 3D-spel zoek ik de grenzen op van wat ik geleerd heb in mijn opleiding en op mezelf. Ik wil met een goed uitgewerkt en leuk spel tegelijk een visitekaartje maken dat kan aantonen dat je aan het eind van deze opleiding klaar bent voor de (indie)game-industrie.


## 2.2 Projectmotivatie (SMART)

### Specifiek

Ik heb ervoor gekozen om alleen aan dit project te werken. Ik werk graag in kleine teams, wat ook de realiteit is in de indie-game-wereld, maar voor het uitwerken van al de ideeen die ik voor dit project in mijn hoofd had, leek het me makkelijker om dit op mezelf te doen. Ik heb hier thuis aan gewerkt, op mijn eigen computer. Ik had thuis alles wat ik nodig had, werken op school of in een andere omgeving had geen meerwaarde.
De planning was om het project te realiseren gedurende het tweede semester. Doordat ik echter een lange periode ziek ben geweest, heb ik mijn deadline moeten opschuiven naar eind augustus.

**MoSCoW-analyse:**

**- Must Have:** 
Mijn minimumdoel was een afgewerkt spel te hebben dat werkt voor soloplay. De speler kan op de muren bewegen, items verzamelen, zichzelf upgraden en de levels ontdekken. De levels zijn bewoond door monsters die door de verschillende ruimtes rondzwerven tot ze een speler zien, waarna ze deze dan achterna gaan. Wat zeker niet mocht ontbreken, is een functioneel scoringsysteem dat ervoor zorgt dat de spelers een quotum hebben dat ze moeten behalen en waarvoor ze dan beloond worden.


**- Should Have:** 
Zodra de basis gelegd was voor een functionele solo-ervaring, ging ik een stapje verder door de multiplayer aspecten toe te voegen en verder uit te werken. Spelers kunnen een lobby hosten waar hun vrienden mee kunnen connecteren. De items en enemies zijn gesynct over het netwerk en gedragen zich hetzelfde voor elke speler.


**- Could Have:** 
Wat de speelwaarde enorm zou verhogen en wat ik dus graag nog extra zou toevoegen, zijn meer verschillende types van enemies, items en omgevingen. Ook de mogelijkheid om speciale items vast te pakken en te gebruiken zoals wapens, is iets wat op termijn zeker nog toegevoegd zou kunnen worden.

**- Won't Have:** 
Ik zou graag dit spel nog verder uitwerken na het afronden van mijn studies, zodat ik de bovenstaande elementen kan implementeren, maar dit zal sterk afhangen van mijn vervolgstudie of job.

### Meetbaar
Ik ben begonnen met het uitwerken van de verschillende functies apart: enemies, loot, speler, ... Ik deed dit in een testomgeving omdat ik de uiteindelijke map later op de planning had staan. Van in het begin zorgde ik ervoor dat ze multiplayer compatibel waren maar verder op dat vlak nog niet volledig afgewerkt. Uiteindelijk zou ik al deze aparte modules samenvoegen tot een geheel. Welke module ik eerst zou maken en welk erna, hing niet echt af van een vaste planning. Ik startte met de speler te laten bewegen en ging vandaar verder met een onderdeel dat dit complementeerde, bijvoorbeeld van player movement -> item dragging -> items + spawning -> environment prefabs.
Ik weet dat dit misschien niet de beste manier is om te werken, maar dit is de manier die voor mijn brein het beste werkt.


### Aanvaard
Dit project past volledig bij de inhoud van de vakken uit mijn opleiding, en zeker bij de afstudeerrichting 3D en XR. Ik heb tijdens mijn opleiding reeds meerdere projecten in VR uitgewerkt, en wou me nu concentreren op de 3D-kant.
Ik pas in dit project alle vaardigheden toe die ik de afgelopen drie jaar geleerd heb. Deze aangeleerde vaardigheden heb ik op mezelf verder uitgediept en uitgebreid met nieuwe kennis, zoals o.a. multiplayer. Ik vind dat je als student van deze richting de lat voor jezelf hoger moet leggen dan wat je aangereikt wordt door je docenten. 

### Realistisch
Het originele concept was volledig wat ik wou realiseren, maar gaandeweg bleek dit niet echt haalbaar. Het bleek te ruim opgezet, met teveel uit te werken modules en variaties. Sommige plannen waren ook te ingewikkeld om gerealiseerd te krijgen of vroegen teveel tijd. Daardoor heb ik wel wat aanpassingen moeten maken aan het originele concept. Medische problemen zorgden er bovendien voor dat ik een aantal maanden niet of nauwelijks aan het project heb kunnen werken.
Om het project gerealiseerd te krijgen, ben ik overgeschakeld van een inventory naar een systeem waarbij de speler de items meesleept aan zichzelf. Ik besloot om dit te doen omdat het creëren van een inventorysysteem op zich al niet heel gemakkelijk is, maar dit nog vele malen moeilijker wordt om het te laten werken in multiplayer. Daarom koos ik ervoor om het aan te passen naar de bovengenoemde manier. Bovendien past dit ook beter bij het verhaal achter het spel.
Verder heb ik voortdurend kleine zaken weggelaten of aangepast, zoals bv. het plan om de speler een grappling hook te geven.


### Tijdgebonden (Planning)
Aanvankelijk was het de bedoeling om dit project te realiseren in het tweede semester. Van deze planning ben ik moeten afwijken door medische redenen.

Voor een concrete weergave van de planning verwijs ik naar sectie 2.3 Projectuitvoering.

## 2.3 Projectuitvoering
- November:
Ik ben deze maand rustig begonnen met wat vooronderzoek naar de techniek en multiplayer-concepten. Ik heb de Unity-projectfile aangemaakt en de basis-instellingen klaargezet, maar echt veel concrete code stond er toen nog niet op papier.
- December:
Tijd voor de eerste echte testjes in Unity! Ik heb een simpele testmap gebouwd om te kijken hoe de verlichting en fysica aanvoelden. Ook stak ik een allereerste proefversie van een speler-controller in elkaar om gewoon wat rond te kunnen lopen.
- Januari:  
In januari ging de knop pas echt om en ben ik vol gas aan het project begonnen. Ik heb heel veel tijd gestoken in het uitproberen van verschillende speler-controllers. Het klimmen op muren en de aangepaste zwaartekracht bleken best pittig om goed te krijgen, dus ik heb meerdere versies geprobeerd totdat de bewegingen eindelijk lekker en stabiel aanvoelden. 
- Februari – Mei:  
Hier was er helaas een flink probleem voor mijn planning. Ik ben tijdens deze maanden een lange periode erg ziek geweest, waardoor ik amper achter mijn laptop kon zitten. De voortgang lag in deze periode helaas bijna compleet stil, wat voor de nodige stress zorgde richting de deadline.
- Juni:   
Gelukkig voelde ik me in juni eindelijk weer fit genoeg om er weer vol tegenaan te gaan! Ik heb de opgebouwde achterstand geprobeerd in te halen door de speler-controller direct om te bouwen. Ik zorgde ervoor dat de movement en de camera-rotaties netjes werkten over het netwerk via Netcode for GameObjects. 
- Juli:  
Ik begon de maand met het maken van een standaard inventarissysteem, maar kwam er al snel achter dat dit in multiplayer niet echt fijn speelde. Ik heb het roer omgegooid en ben geswitcht naar een fysica-gebaseerd tether-systeem waarmee je spullen met een touw kunt meeslepen. Daarna heb ik meteen de eerste items gemaakt die je in de speelwereld kunt verzamelen. 
- Augustus:  
    Dit werd de absolute sprintmaand waarin alles samenkwam:
    - AI & Monsters: ik heb de Finite State Machine gebouwd voor de vijanden, met toestanden voor Idle, Patrol, Chase en Attack. 
    - Stats & Shop: ik voegde speler-statistieken toe (zoals snelheid en draagkracht) en koppelde deze aan een winkel-systeem. Zodra spelers items inleveren, krijgen ze geld om upgrades te kopen. 
    - Procedural Map: van mijn assetpack (Leartes Studios, 2023) maakte ik losse kamers en gangen, die een script nu willekeurig aan elkaar plakt om elke run een nieuwe map te vormen.
    - Procedural Animation: ik verving het speler-model door een spinnenrobot en zorgde via raycasts en Inverse Kinematics dat de poten zich automatisch aanpassen aan de muur waar je op klimt. 
    - Integratie: tenslotte heb ik een main menu gebouwd, alle losse onderdelen aan elkaar gekoppeld en de game speelbaar gemaakt!



## 2.4 Projectconcretisering

### 2.4.1 Inleiding
Het hoofddoel van dit bachelorproject was het ontwerpen, architectureren en opleveren van *Lootbugs*, een coöperatieve 3D-multiplayer survival-roguelike. In dit genre worden spelers in een vijandige, procedureel gegenereerde omgeving geplaatst. De kern van het spel draait om het verkennen van een verlaten sci-fi complex, het verzamelen van waardevolle grondstoffen ("buit"), het overleven van dynamische gevaren en het op tijd terugkeren om de 'Queen' te voeden.

De grootste technische uitdaging lag in het integreren van uiteenlopende deelsystemen binnen de Unity-engine. Het project vereiste onder meer:
- een op maat gemaakte spelerbesturing met een aangepast zwaartekrachtsysteem.
- een solide netwerkarchitectuur via Unity Netcode for GameObjects.
- geavanceerde kunstmatige intelligentie (AI) aangestuurd door een Finite State Machine.
- procedurele wereldgeneratie en fysica-gebaseerd verzamelen.
- procedurele spin-animaties door middel van raycasting en Inverse Kinematics (IK).

In de volgende paragrafen worden deze architectuurkeuzes besproken, met verwijzingen naar de logica uit de broncode.

---

### 2.4.2 Technische Uitwerking en Systeemarchitectuur

**2.4.2.1 Concept en vergelijking**

Het concept van *Lootbugs* is geïnspireerd door coöperatieve horror-roguelikes zoals *Lethal Company* (Zeekerss, 2023) en *R.E.P.O.* (semiwork, 2025). Deze games combineren een hoog risico-beloningssysteem met onvoorspelbare situaties die ontstaan doordat spelers onder druk moeten samenwerken. 

Waar eerdere projecten tijdens mijn opleiding zich richtten op singleplayer VR-ervaringen, verschuift de focus hier naar een multiplayer-omgeving op de pc. Deze overstap betekende dat alle fysische simulaties, spelerstatistieken, camerabewegingen en interacties vanaf de basis ontworpen moesten worden met concepten als netwerkeigenaarschap en netwerkautoriteit als uitgangspunt.

**2.4.2.2 Spelerbeweging & Netwerkintegratie**

Het bewegingssysteem vormt de technische ruggengraat van het spel. Om de speler echt als een mechanische robot te laten voelen, is er een fysica-gebaseerd script geschreven waarmee de speler over de grond, tegen schuine hellingen en op verticale muren kan lopen.

***Oppervlakte- en normaal-detectie:***

In het script van de speler is de standaard Unity-zwaartekracht op de `Rigidbody` uitgeschakeld. In plaats daarvan berekent het script continu een eigen 'boven-as', gebaseerd op de richting van de zwaartekracht. 
- Wanneer de speler over een oppervlak beweegt, evalueert de methode `EvaluateCollision` de botsingen. Dit gebeurt door het inproduct (*dot product*) te berekenen tussen de hoek van de ondergrond (de omgevingsnormaal) en de lokale boven-as. Valt deze hoek binnen de ingestelde limieten, dan is het oppervlak begaanbaar.
- Om te voorkomen dat de speler tijdens het afdalen van een muur valt, voert de controller via `SnapToGround` een neerwaartse meting (raycast) uit. Zodra er een geschikt oppervlak wordt gevonden, past het script de oriëntatie aan en 'kleeft' de speler aan de ondergrond.
- Via `AdjustVelocity` worden de toetsenbordinvoer en snelheid geprojecteerd op het vlak van het oppervlak. Hierdoor kan de speler altijd vloeiend in elke richting sturen, ongeacht de kanteling van de muur.

**2.4.2.3 Netwerkperspectief en Camera-ontkoppeling**

Om vertraging op het netwerk tegen te gaan, werkt de besturing volgens het principe van *Client-Side Ownership*. Alleen de speler die eigenaar is van de robot verwerkt de muis- en toetsenbordinvoer. De camera is hiervoor opgesplitst in drie losse componenten:

1. **Camera Input:** Dit script draait lokaal. Het vergrendelt de muis en vertaalt de muisbewegingen naar lokale camerarotaties, waarbij de kijkhoek naar boven en beneden wordt begrensd.

2. **Camera Volgsysteem:** Een lichtgewicht netwerkscript dat de positie en rotatie van de daadwerkelijke Unity-camera continu koppelt aan een ankerpunt in het spelermodel.

3. **Speler-rotatie:** Dit zorgt ervoor dat het 3D-model van de speler synchroon meedraait met de camera. Hierdoor zien andere spelers in het netwerk exact welke kant iemand opkijkt.

**2.4.2.4 Vijandelijke AI, State Pattern & Levenscyclus**

De vijanden gebruiken een AI-systeem gebaseerd op het *State Pattern*, aangestuurd door een *Finite State Machine (FSM)*. Dit zorgt voor gestructureerd en schaalbaar gedrag.

Het AI-component draait op de server. Het activeert het navigatiesysteem, de animaties en de startstatus. Elke frame roept dit script de `Process()`-methode van de huidige status aan. Elke specifieke status erft eigenschappen van de basisklasse `State` en doorloopt de volgende cyclus:
*   **Enter:** Wordt één keer uitgevoerd bij de start (bijv. snelheid of animaties instellen), waarna de cyclus overgaat naar de update-fase.
*   **Update:** Verwerkt elke frame de logica. Zodra aan een voorwaarde wordt voldaan (bijv. speler gezien), wordt de nieuwe status klaargezet in `nextState` en schuift de cyclus door naar *Exit*.
*   **Exit:** Ruimt gegevens op en geeft de `nextState` door aan het hoofdscript voor een vlotte overgang.

```text
+-----------------------------------+
| AI MonoBehaviour                  |
| (currentState.Process() in Update)|
+-----------------+-----------------+
                  |
                  v
+-----------------------------------+
| State Base                        |
| - Enter()  -> Event.Update        |
| - Update() -> Evalueert logica    |
| - Exit()   -> Returns nextState   |
+-----------------+-----------------+
                  |
  +---------------+---------------+
  |               |               |
  v               v               v
+------------+  +------------+  +---------------+
| Idle State |  | Patrol     |  | Chase State   |
| (Rust)     |  | (Route)    |  | (Achtervolgen)|
+------+-----+  +------+-----+  +-------+-------+
       |               |                |
       +---------------+----------------+
                       |
                       v
                +--------------+
                | Attack State |
                | (Aanvallen)  |
                +--------------+
```

De basisklasse bevat ook gedeelde detectiesystemen:

- **Visuele Detectie:** Zoekt spelers in de scène. Als de afstand en de hoek tussen het monster en de speler binnen de zichtlijnen vallen, wordt de speler het doelwit.
- **Aanvalsdetaillering:** Controleert of de speler dichtbij genoeg is om aan te vallen.

**2.4.2.5 Interactie, Stofzuigersysteem & Tethering**

Omdat een traditioneel inventarismenu slecht werkt bij netwerkvertraging, is er een fysiek en hybride verzamelsysteem ontworpen. 
*   **Ontkoppeling:** Om te voorkomen dat de spelercode afhankelijk wordt van alle verschillende soorten objecten in de wereld, wordt de interface `IInteractable` gebruikt. Deze verplicht objecten om een `Interact()`-methode en een `InteractionText`-eigenschap te bevatten.
*   **Stofzuigersysteem:** Dit script werkt in twee stappen. Eerst detecteert het voorwerpen (en voegt ze toe aan een lijst), daarna trekt het deze voorwerpen via fysica vloeiend naar de speler toe met `Vector3.MoveTowards`.
*   **Netwerk Tethering:** Spelers kunnen objecten ook aan zich vastmaken met virtuele touwen (tethers). Via netwerkcommando's wordt dit gesynchroniseerd. Een fysieke elastische verbinding  zorgt voor de daadwerkelijke sleepkracht en de visuele weergave.

**2.4.2.6 Waarde en Gewicht van Buit**


Voorwerpen hebben gesynchroniseerde variabelen voor zeldzaamheid, puntenwaarde en gewicht. Bij het genereren bepaalt de server de zeldzaamheid op basis van een kansberekening:

| Zeldzaamheid | Kans | Punten | Gewicht |
| :--- | :--- | :--- | :--- |
| Common | 60 | 10 | 0.5f |
| Uncommon | 25 | 25 | 1.0f |
| Rare | 10 | 50 | 1.5f |
| Epic | 4 | 100 | 2.0f |
| Legendary | 1 | 500 | 2.5f |

Het meeslepen van buit beïnvloedt de snelheid van de speler. In het spelerscript wordt de snelheid continu herberekend met de volgende formule:

`EffectieveSnelheid = max(MinSnelheid, BasisSnelheid - (TotaalGewicht / SpelerKracht))`

**2.4.2.7 Procedurele Wereldgeneratie & Item Spawning**

Om het spel herspeelbaar te maken, bouwt de server bij aanvang automatisch de plattegrond op met een 'dungeon-growth' algoritme (vanuit een startpunt breiden de kamers zich iteratief uit). Zodra de wereld staat, wordt het navigatienetwerk voor de vijanden berekend (*Runtime Bake* van de `NavMesh`). Daarna activeert de `ItemSpawner`, die de buit willekeurig in de wereld plaatst en over het netwerk synchroniseert.

**2.4.2.8 Procedurele Animaties & Gronddetectie**

De speler bestuurt een robot-spin. Om te zorgen dat de poten zich visueel perfect aanpassen aan de vloeren en muren, worden continu raycasts uitgevoerd vanaf de poten om ze aan het oppervlak te ankeren (*Inverse Kinematics*). De `BodyController` beheert de stap-logica via lineaire interpolatie. Om de poten tijdens een stap in een mooie boog te laten bewegen, wordt een sinusfunctie gebruikt:

`HoogteOffset = sin(t × π) × 0.3`

**2.4.2.9 Progressie, Scores & Economie**

Spelers moeten hun buit succesvol inleveren. De server controleert of dit correct gebeurt en kent vervolgens de score toe. Dit saldo kan de speler gebruiken voor upgrades zoals Health, Speed, Stamina, Strength en Gains. Na elke aankoop stijgt de prijs van een upgrade progressief met 20%.

---

### 2.4.3 Slot
De technische uitwerking van *Lootbugs* laat zien dat uiteenlopende systemen binnen Unity kunnen samenkomen tot één functioneel geheel. Dit varieert van netwerk-gebaseerde spelerfysica en ontkoppelde camera-architectuur tot AI via een Finite State Machine, procedurele spin-animaties en server-geautoriseerde puntentelling.

Door strategische keuzes te maken — zoals het ontkoppelen van objecten via de `IInteractable`-interface en het vervangen van een klassieke inventory door een `ItemCollector`- en tether-systeem — is een robuuste, schaalbare multiplayergame gerealiseerd. Het resultaat voldoet aan de technische vereisten van een modern 3D-multiplayer bachelorproject.


## 2.5 Projectconclusie
Ik ben tevreden met het resultaat van mijn bachelorproject. Het spel is niet helemaal geworden zoals ik het oorspronkelijk in mijn hoofd had, maar het komt er aardig in de buurt en de uiteindelijke versie is iets waar ik trots op ben. 
Ik wou een multiplayer spel maken, en dat was een hele uitdaging. Hier heb ik vaak mijn tanden op stuk gebeten. Ook de inventory gaf aanvankelijk veel problemen. Hier hangen zoveel aspecten aan vast (bijwerken, zichtbaar houden, item-informatie opslaan, …) en ik had ook al snel door dat dit met een multiplayer problemen zou opleveren. Ik heb dit opgelost door een ander systeem toe te passen. Ook het movementsysteem liep in het begin niet goed. Hier heb ik heel veel tijd aan besteed.

Aan het implementeren van de procedural animation van de spinnenpoten heb ik veel plezier gehad. Dit had ik nog nooit eerder gedaan, maar ik wou dit zeker erin verwerken. Het is iets wat je niet veel tegenkomt in spellen.  
Een spel is echter nooit af, en mijn spel zeker niet. Het movementsysteem kan nog veel beter, soepeler en vooral het lopen op de muren is niet volledig zoals ik het zou willen. Verder vind ik het aantal enemy AI’s nog te beperkt. Ik wil meer verschillende enemy AI’s ontwikkelen, vooral op het vlak van gedrag. Op dit moment loopt een enemy gewoon achter de spelers aan die in zijn zichtveld komen. Dit kan nog beter uitgewerkt worden. In de toekomst wil ik zeker ook nog meer verschillende stijlen van spelomgevingen toevoegen, zodat de spelomgeving gevarieerder wordt.

Ik heb enorm veel geleerd door het ontwikkelen van Lootbugs. Mijn kennis van Unity is veel uitgebreider geworden, en dan vooral de multiplayer kant. Hier had ik nog nooit mee gewerkt en ben ik dus volledig van nul mee moeten starten. Ook nieuw voor mij was het gebruik van state machines voor het gedrag van de enemy AI’s. En, zoals hierboven vermeld, de procedural animation. Op technisch vlak zijn dit vooral de grote zaken die eruit springen. Hierbovenop heb ik natuurlijk ook mijn coding skills verbeterd en heel veel geleerd over het correct gebruiken van AI bij het coderen.

Op persoonlijk vlak ben ik door dit project ook gegroeid. Het is voor mij een hele uitdaging om een langdurige planning op te zetten en me hieraan te houden. Dit liep wel eens mis. Ik heb leren volhouden en een probleem uitspitten tot ik de oplossing gevonden had. Opzoeken, uitproberen, weggooien en opnieuw beginnen, … Het zijn allemaal dingen waar ik stappen in heb gezet. Ik nam in het verleden bij opdrachten soms wel eens genoegen met de gemakkelijkste weg, maar dit was bij dit project niet het geval. Tenzij de gemakkelijkste oplossing de beste oplossing was.

Ik ben blij dat ik dit project mocht realiseren. Het heeft me doen groeien als mens en als game-developer.

<div style="page-break-after: always;"></div>
# 3 Bibliografie
- Code Monkey. (2021, 22 november). Simple Procedural Animation in Unity Tutorial. YouTube. https://www.youtube.com/watch?v=EdjAYrssxDM

- Flick, J. (2020a, 22 februari). Custom Gravity. Catlike Coding. https://catlikecoding.com/unity/tutorials/movement/custom-gravity/

- Flick, J. (2020b, 14 maart). Climbing. Catlike Coding. https://catlikecoding.com/unity/tutorials/movement/climbing/

- Leartes Studios. (2023). Cyberpunk Gigapack - Modular Environment, Characters, Vehicles, Weapons & Props (Versie 1.0) [3D Asset Pack]. Unity Asset Store. https://assetstore.unity.com/packages/3d/environments/sci-fi/cyberpunk-gigapack-modular-environment-characters-vehicles-weapo-256798

- Nystrom, R. (2014). Game programming patterns. Genever Benning.

- Pelckmans, P. (2020). Refereren en bibliograferen: een leidraad. Hogeschool MCT.

- Sketchfab. (z.d.). 3D Robot Models [Online repository voor 3D-modellen]. Van https://sketchfab.com/search?features=animated&q=robots&sort_by=-likeCount&type=models

- Unity Technologies. (z.d.). Finite State Machines in Unity. Unity Learn. Van https://learn.unity.com/course/finite-state-machines-1

- Unity Technologies. (2023a). About Netcode for GameObjects (Versie 1.5) [Software documentatie]. Unity Documentation. https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@1.5/manual/index.html

- Unity Technologies. (2023b). Inverse Kinematics (IK) [Software documentatie]. Unity Manual. https://docs.unity3d.com/Manual/InverseKinematics.html

- Zeekerss. (2023). Lethal Company [Video Game]. Steam.

- semiworks. (2025). R.E.P.O. [Video Game]. Steam.
---

<div style="page-break-after: always;"></div>
# 4 Bijlagen

## Bijlage 1: Codekaart

Het is belangrijk te beseffen dat de onder de verschillende evaluatieonderdelen opgesomde criteria niet worden beschouwd als een afvinklijstje. De evaluatieonderdelen worden holistisch benaderd en beoordeeld.

**1. Opmaak (/5)**
* **Op1** Voorblad (sjabloon) ontbreekt of is onvolledig
* **Op2** Inhoudsopgave ontbreekt of is onvolledig
* **Op3** Typografie is inconsequent
* **Op4** Titels zijn onvoldoende herkenbaar of slecht gemarkeerd
* **Op5** Nummering loopt spaak

<table border="1">
  <tr>
    <td style="width: 150px;text-align: center;">0</td>
    <td style="width: 150px;text-align: center;">2.5</td>
    <td style="width: 150px;text-align: center;">[5]</td>
  </tr>
</table>

**2. Structuur (/15)**

**2.1. Globale tekststructuur (/10)**
* **St1** Opgelegde structuur wordt niet nagevolgd
* **St2** Een van de opgelegde onderdelen (projectomschrijving, projectuitvoering, projectconcretisering, projectconclusie) ontbreekt
* **St3** Indeling in hoofdstukken en paragrafen is onduidelijk
* **St4** Te weinig structuuraanduidingen

<table border="1" >
  <tr>
    <td style="width: 150px;text-align: center;">0</td>
    <td style="width: 150px;text-align: center;">5</td>
    <td style="width: 150px;text-align: center;">[10]</td>
  </tr>
</table>

**2.2. Alinea's (/5)**
* **St5** Onoverzichtelijke indeling in alinea's
* **St6** Nieuwe aspecten krijgen geen nieuwe alinea
* **St7** Zwakke verbanden tussen de alinea's (concreet)

<table border="1" >
  <tr>
    <td style="width: 150px;text-align: center;">0</td>
    <td style="width: 150px;text-align: center;">2.5</td>
    <td style="width: 150px;text-align: center;">[5]</td>
  </tr>
</table>

**3. Referenties (/15)**
* **Ref1** Citaten vallen niet op
* **Ref2** Citaten worden onvoldoende gekaderd
* **Ref3** Te weinig bronnen geraadpleegd (min. 5)
* **Ref4** Bronvermeldingen ontbreken of zijn vaag
* **Ref5** Bronnen zijn niet up-to-date
* **Ref6** Literatuurlijst ontbreekt of is onvolledig

<table border="1" >
  <tr>
    <td style="width: 150px;text-align: center;">5</td>
    <td style="width: 150px;text-align: center;">10</td>
    <td style="width: 150px;text-align: center;">[15]</td>
  </tr>
</table>

**4. Inhoud (/55)**
Beoordeeld op basis van de onderwijskwalificaties die binnen het Vlaams Kwalificatieraamwerk worden vastgelegd voor de bacheloropleidingen (VKS 6)*.

**4.1. Duidelijkheid (/15)**
* **Du1** Tekst is onduidelijk op niveau zinsbouw (micro)
* **Du2** Tekst als geheel is niet duidelijk (macro)
* **Du3** Hoofdgedachte komt niet over
* **Du4** Rode draad is onduidelijk
* **Du5** Vage formulering

<table border="1" >
  <tr>
    <td style="width: 150px;text-align: center;">5</td>
    <td style="width: 150px;text-align: center;">10</td>
    <td style="width: 150px;text-align: center;">[15]</td>
  </tr>
</table>

**4.2. Relevantie (/25)**
* **Rel1** Onderwerp wordt onvoldoende uitgediept
* **Rel2** Nil novi sub sole
* **Rel3** Te weinig persoonlijke verwerking
* **Rel4** Overtuigt niet
* **Rel5** Kritisch

<table border="1" >
  <tr>
    <td style="width: 100px;text-align: center;">5</td>
    <td style="width: 100px;text-align: center;">10</td>
    <td style="width: 100px;text-align: center;">15</td>
    <td style="width: 100px;text-align: center;">[20]</td>
    <td style="width: 100px;text-align: center;">25</td>
  </tr>
</table>

**4.3. Zakelijkheid (/15)**
* **Za1** Geen academische stijl
* **Za2** Onvoldoende gebruik van vakterminologie

<table border="1" >
  <tr>
    <td style="width: 150px;text-align: center;">5</td>
    <td style="width: 150px;text-align: center;">[10]</td>
    <td style="width: 150px;text-align: center;">15</td>
  </tr>
</table>

**5. Correctheid (/10)**
* **Cor1** Spellingfouten
* **Cor2** Stijlfouten (...)
* **Cor3** Fouten tegen interpunctie

<table border="1" >
  <tr>
    <td style="width: 150px;text-align: center;">0</td>
    <td style="width: 150px;text-align: center;">5</td>
    <td style="width: 150px;text-align: center;">[10]</td>
  </tr>
</table>

---

*\* VKS niveau 6 (https://vlaamsekwalificatiestructuur.be)*

<table border="1" >
  <th style="width: 300px;text-align: center">
    Kennis en vaardigheden
  </th>
  <th style="width: 300px;text-align: center;">
    Context, autonomie en verantwoordelijkheid
  </th>
  <tr>
    <td>kennis en inzichten uit een specifiek domein kritisch evalueren en combineren;</td>
    <td>handelen in complexe en gespecialiseerde contexten;</td>
  </tr>
  <tr>
    <td>complexe gespecialiseerde vaardigheden toepassen, gelieerd aan onderzoeksuitkomsten;</td>
    <td>functioneren met volledige autonomie en een ruime mate van initiatief</td>
  </tr>
  <tr>
    <td>relevante gegevens verzamelen en interpreteren, en geselecteerde methodes en hulpmiddelen innovatief aanwenden om niet-vertrouwde complexe problemen op te lossen.</td>
    <td></td>
  </tr>
</table>

---

## Bijlage 2: Moodboard, screenshots en links
https://drive.google.com/drive/folders/1JTIc_0eYO9MlJ8BloKsSSod0G8Bw5OOy?usp=drive_link

---