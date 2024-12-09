# FileGuard - Documentazione Completa
Versione 1.0 - Novembre 2024

## 1. Introduzione
FileGuard è un'applicazione personale per monitorare e versionare i file. Questo documento spiega il funzionamento e lo sviluppo del software in modo semplice e pratico.

## 2. Come Funziona

### Per l'Utente
1. **Avvio e Selezione**
   - Apri FileGuard
   - Clicca "Sfoglia"
   - Seleziona cartelle/file da monitorare
   
2. **Monitoraggio**
   - L'app controlla le modifiche ai file
   - Per ogni modifica:
     - Crea versione temporanea
     - Mostra popup con opzioni:
       - "Sostituisci" (elimina versioni precedenti)
       - "Mantieni Entrambi" (aggiunge alla lista)
       - "Annulla"

3. **Visualizzazione**
   - Sinistra: Tree dei file monitorati
   - Destra: Tabella modifiche con:
     - Data/Ora
     - Tipo modifica
     - Percorso
     - Riassunto modifica
   - Pulsante "Torna Indietro" per ripristino

### Limiti e Configurazioni
- Massimo 3 versioni per file (modificabile)
- Pulizia automatica versioni temp dopo 24h
- Solo per uso personale
- Interfaccia semplice e intuitiva

## 3. Struttura Sviluppo

### Organizzazione Cartelle
```
D:\FileGuard\
├── Core\          # Interfaccia principale
├── Modules\       # Componenti separati
└── Build\         # File compilati
```

### Tasks di Sviluppo
1. **Task 1: Core UI**
   - Interfaccia base
   - Definizione API per moduli
   - Punti di integrazione

2. **Task 2: Monitor**
   - Sistema monitoraggio file
   - Comunicazione con UI

3. **Task 3: Versioning**
   - Gestione versioni
   - Backup e ripristino

## 4. Lezioni dalle Versioni Precedenti

### Problemi Riscontrati
1. **Percorsi File**
   - ❌ Percorsi relativi causavano errori
   - ✅ Soluzione: Solo percorsi assoluti centralizzati

2. **File Duplicati**
   - ❌ Copie sparse nel sistema
   - ✅ Soluzione: Struttura cartelle fissa

3. **Tasks Frammentati**
   - ❌ Troppi task piccoli creavano confusione
   - ✅ Soluzione: Task più ampi e autocontenuti

## 5. Mockup UI

### Interfaccia Principale
```
+------------------------+-------------------------+
|  [Browse...]           |  Timestamp   Type Path |
|  + Monitored          |  2024-11-25  Mod  doc1 |
|    - file1.txt        |  2024-11-24  New  doc2 |
|    - file2.doc        |                        |
|                       |  [Torna Indietro]      |
+------------------------+-------------------------+
```

### Popup Modifica
```
+----------------------------------+
|  File Modificato: doc1.txt       |
|  [Sostituisci] [Mantieni] [X]   |
+----------------------------------+
```

## 6. Sviluppo con VS Code e Claude 3

### Processo di Sviluppo
1. Task 1 crea struttura base
2. Altri task lavorano su moduli isolati
3. Integrazione tramite API definite

### Prevenzione Errori
- Configurazione percorsi centralizzata
- Testing dopo ogni modulo
- Backup regolari

## 7. Priorità Sviluppo

### Fase 1: Interfaccia
- TreeView file
- Tabella modifiche
- Popup base

### Fase 2: Monitoraggio
- Sistema watch file
- Gestione eventi

### Fase 3: Versioning
- Salvataggio versioni
- Sistema ripristino

## 8. Suggerimenti per l'Uso

### Best Practices
- Monitorare poche cartelle importanti
- Verificare spazio disco
- Controllare versioni regolarmente

### Manutenzione
- Pulizia versioni vecchie
- Backup configurazioni
- Verifica file monitorati

## 9. Archivio

### Struttura
```
D:\FileGuard\Archive\
├── 2024\          
│   ├── Gennaio\   # Organizzato per data
│   └── Febbraio\  
└── Search\        # Indice per ricerca
```

### Funzionalità
- Vista cronologica file
- Ricerca per nome/data
- Ripristino versioni
- Pulizia automatica

## 10. Metodologia Sviluppo

### Isolamento Tasks
1. Task 1 (Core):
   - Crea struttura base
   - Definisce API/interfacce
   - NO accesso moduli altri task

2. Altri Task:
   - Lavorano solo nelle proprie cartelle
   - Comunicano via API definite
   - NO modifica file altri task

### Esempio Pratico
- Task 1 crea `IFileMonitor.cs`
- Task 2 implementa monitor in cartella separata
- All'integrazione: solo DLL finali

## 11. Supporto

### Problemi Comuni
- File bloccati → Attendere sblocco
- Spazio insufficiente → Pulizia versioni
- File non trovato → Riavvio monitoraggio

### Verifiche
- Permessi cartelle
- Spazio disponibile
- File non corrotti
