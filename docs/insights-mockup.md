# Mockup — sezione Insight

## Obiettivo

La sezione **Insight** aiuta a capire come stanno cambiando le spese, senza trasformare l'app in uno strumento di pianificazione del budget. Mostra confronti chiari fra periodi e suggerimenti generati esclusivamente dai dati presenti sul dispositivo.

Questa proposta copre soltanto:

- andamento delle spese;
- confronto fra categorie;
- selettore del periodo `Questo mese` / `Questo anno`;
- suggerimenti locali basati sullo storico.

Sono esplicitamente esclusi alert di budget configurabili, soglie, notifiche, impostazioni e qualsiasi UI relativa al budget.

## Linguaggio visivo

La nuova tab riusa il linguaggio MAUI già presente:

- sfondo `#FAFBF8`;
- verde primario `#196D61` per selezione e azioni;
- contenitore menta `#DDF4EB` per il riepilogo principale;
- card bianche con bordo tenue `#DCE5DF` e raggio 20;
- testo scuro `#1A2521` e testo secondario `#66736D`;
- Open Sans: titolo 28, titoli di sezione 22, titoli card 16, corpo 14.

I contenuti scorrono verticalmente con margini orizzontali di 22. Le aree attivabili hanno almeno 48 × 48 dp. Le icone sono di supporto al testo, non l'unica modalità per comprendere un'informazione.

## Struttura della schermata

La sezione è una voce principale della barra inferiore, etichettata **Insight**. L'icona suggerita è un grafico a barre. Non deve sostituire le sezioni esistenti: la barra inferiore conserva quattro tab, `Oggi`, `Insight`, `Categorie` e `Impostazioni`. `Oggi` resta dedicata alle spese del mese e all'inserimento rapido.

```text
┌──────────────────────────────────────┐
│ Insight                               │
│ Guarda come cambiano le tue spese.    │
│                                      │
│     [ Questo mese ▾ ]                 │
│                                      │
│ ┌──────────────────────────────────┐ │
│ │ QUESTO MESE                       │ │
│ │ 486,20 €                          │ │
│ │ +12% rispetto a giugno            │ │
│ │ ▁▃▂▅▃▆▇  Andamento giornaliero     │ │
│ └──────────────────────────────────┘ │
│                                      │
│ I tuoi insight                       │
│ ┌──────────────────────────────────┐ │
│ │ ↑ Spese in aumento                │ │
│ │ Hai speso 52,00 € in più per...   │ │
│ │                         Vedi spese│ │
│ └──────────────────────────────────┘ │
│                                      │
│ Per categoria                        │
│ ┌──────────────────────────────────┐ │
│ │ Alimentari              182,40 €  │ │
│ │ ████████████████  38%   +18%      │ │
│ ├──────────────────────────────────┤ │
│ │ Trasporti                96,00 €  │ │
│ │ ████████                20%   −8% │ │
│ └──────────────────────────────────┘ │
│                                      │
│ Oggi    Insight    Categorie  Impostaz.│
└──────────────────────────────────────┘
```

Su schermi stretti le quattro voci restano visibili: le etichette possono ridursi a `Impostaz.` e il testo non deve sovrapporsi. La tab attiva usa colore verde e un'indicazione testuale o di forma aggiuntiva; quelle inattive usano il testo secondario. La barra rispetta la safe area inferiore.

### Intestazione e periodo

- Titolo: `Insight`.
- Sottotitolo: `Guarda come cambiano le tue spese.`
- Sotto l'intestazione, il pulsante testuale `Questo mese ▾`, centrato come l'etichetta del mese nella schermata Oggi.
- Un tocco apre un menu o un bottom sheet con due opzioni mutuamente esclusive: `Questo mese` e `Questo anno`. L'opzione attiva ha segno di spunta e colore verde.
- Il cambio aggiorna tutte le card e conserva la posizione di lettura all'inizio della schermata. Non richiede rete e non modifica alcun dato.

### Riepilogo dell'andamento

Card tonale menta, subito dopo il periodo:

- etichetta maiuscola: `QUESTO MESE` oppure `QUESTO ANNO`;
- totale del periodo, per esempio `486,20 €`;
- confronto con il periodo precedente equivalente: `+12% rispetto a giugno` o `−8% rispetto allo scorso anno`;
- mini-grafico a linea o barre, senza gesti obbligatori, con testo alternativo: `Andamento giornaliero: picco di 74,50 € il 18 luglio`.

Per un mese concluso il confronto è con l'intero mese precedente; per il mese in corso confronta soltanto i giorni trascorsi con gli stessi giorni del mese precedente. Per un anno concluso il confronto è con l'intero anno precedente; per l'anno in corso confronta soltanto i mesi conclusi più i giorni trascorsi nel mese corrente con la medesima porzione dell'anno precedente. Il grafico usa giorni nel mese e mesi nell'anno.

Se non esiste un periodo equivalente o non ci sono dati sufficienti per un confronto equo, mostra `Continua a registrare le spese per confrontare questo periodo` invece di una percentuale. Gli insight comparativi vengono rimandati; restano disponibili il totale, l'andamento del periodo e le categorie.

## Tipi di insight

La sezione `I tuoi insight` è ordinata per utilità: prima le variazioni più rilevanti, poi le osservazioni descrittive. Mostra al massimo tre card; il resto è disponibile con `Mostra tutti gli insight` se necessario. Ogni card contiene icona, titolo, spiegazione breve e un'azione contestuale.

| Tipo | Regola descrittiva | Esempio di contenuto | Azione |
| --- | --- | --- | --- |
| Spese in aumento | Una categoria cresce in valore e incidenza rispetto al periodo equivalente e confrontabile. | `Spese in aumento` — `Hai speso 52,00 € in più per Alimentari rispetto a giugno.` | `Vedi spese`: nel mese apre `CategorySpendingPage`; nell'anno richiede un futuro dettaglio annuale filtrato. |
| Spese in diminuzione | Una categoria cala in modo rilevante rispetto al periodo equivalente e confrontabile. | `Meno spese nei trasporti` — `Hai speso il 18% in meno per Trasporti rispetto a giugno.` | `Vedi spese`: nel mese apre `CategorySpendingPage`; nell'anno richiede un futuro dettaglio annuale filtrato. |
| Categoria principale | Una categoria è la maggiore quota del totale nel periodo. | `La categoria più importante` — `Alimentari rappresenta il 38% delle spese di luglio.` | `Esplora categoria`: nel mese apre `CategorySpendingPage`; nell'anno richiede un futuro dettaglio annuale filtrato. |
| Nuova abitudine | Una categoria o un tag prima assente compare più volte nel periodo corrente. | `Una nuova abitudine` — `Hai registrato 4 spese in Abbonamenti questo mese.` | `Vedi spese`: nel mese apre `CategorySpendingPage`; nell'anno richiede un futuro dettaglio annuale filtrato. |
| Spesa insolita | Una singola spesa è sensibilmente superiore alle altre spese della stessa categoria o allo storico personale. | `Una spesa da rivedere` — `245,00 € per Auto è la spesa più alta del mese.` | `Apri spesa`. |
| Andamento stabile | Non emergono variazioni importanti e ci sono dati confrontabili. | `Spese stabili` — `Il totale è simile a giugno: variazione del 2%.` | Nessuna azione necessaria. |

Le frasi devono essere prudenti: descrivono i dati (`hai speso`, `rappresenta`) e non giudicano il comportamento dell'utente. Non usare messaggi riferiti a obiettivi, limiti o budget.

## Confronto per categoria

La sezione `Per categoria` segue gli insight e contiene una card unica o più card bianche. Le categorie sono ordinate per importo decrescente nel periodo selezionato.

Ogni riga include:

- icona e nome categoria;
- importo, ad esempio `182,40 €`;
- barra di incidenza rispetto al totale del periodo, con percentuale testuale, ad esempio `38% del totale`;
- variazione leggibile rispetto al periodo equivalente: `+18% in più`, `−8% in meno`, oppure `nessuna variazione rilevante`, nel testo secondario neutro;
- area completa della riga selezionabile con descrizione accessibile: `Alimentari, 182,40 euro, 38% del totale, 18% in più rispetto a giugno. Mostra le spese della categoria.`

Con `Questo mese`, un tocco apre la vista di dettaglio esistente `CategorySpendingPage`. Con `Questo anno`, l'azione porta a un futuro dettaglio annuale filtrato, da progettare e implementare prima di rendere disponibile il percorso. Le barre usano il colore come rinforzo: importo, percentuale e variazione restano sempre testuali. Aumenti e diminuzioni usano lo stesso trattamento cromatico neutro e sono espressi dalle parole `in più` e `in meno`, non da un colore valutativo.

## Stati vuoti e casi con pochi dati

| Situazione | Contenuto mostrato |
| --- | --- |
| Nessuna spesa nel periodo selezionato | Icona discreta, titolo `Ancora nessuna spesa`, testo `Aggiungi una spesa per iniziare a vedere i tuoi insight.` e azione primaria `Aggiungi spesa`. |
| Dati nel periodo ma nessun confronto equo possibile | Mostra totale, categorie e testo `Continua a registrare le spese per confrontare questo periodo.` Gli insight comparativi non appaiono. |
| Una sola categoria | Mostra la categoria e la relativa quota; evita confronti o classifiche artificiose. |
| Dati insufficienti per suggerimenti | Mantiene il riepilogo e mostra `Stiamo imparando dalle tue spese sul dispositivo.` senza spinner né richieste di consenso aggiuntive. |
| Errore di lettura locale | Card bianca con testo `Non riesco a caricare gli insight. Riprova tra poco.` e azione `Riprova`. |

## Privacy, interazioni e accessibilità

- Il calcolo avviene solo sul dispositivo e la schermata lo comunica con la nota `I tuoi insight vengono elaborati solo su questo dispositivo.` alla fine della lista o in un pannello informativo iniziale richiudibile.
- Gli insight non inviano dati, non richiedono account e non usano servizi esterni.
- Titoli e sezioni espongono i rispettivi livelli semantici; grafici e icone hanno una descrizione testuale equivalente.
- Le variazioni non dipendono da colore, frecce o simboli: la frase completa indica `in più` o `in meno`, con trattamento cromatico neutro per entrambe.
- Il selettore del periodo, le card apribili e le azioni sono raggiungibili da lettore di schermo e tastiera, con etichette in italiano.
- Rispettare il ridimensionamento dei caratteri: le righe possono crescere in altezza, gli importi non devono sovrapporsi e il grafico può passare sotto il testo.

## Fuori ambito

Questo mockup non introduce budget o alert: nessun importo-obiettivo, barra di avanzamento verso un budget, superamento, notifica, configurazione o suggerimento basato su un limite di spesa.
