# Benutzerhandbuch

## Zweck des Programms

VIBN Tools bündelt Werkzeuge für virtuelle Inbetriebnahme, FEE-/Simulationsprojekte und die ViCo-Arbeitsabläufe in einer WPF-Desktopanwendung. Die bisherigen VIBN-Werkzeuge bleiben eigenständige Reiter. ViCo ergänzt PC-/Projektsuche, Remote-Zugriff, Favoriten, Transfer, TIA Portal und Verwaltung.

## Grundbedienung

Die Hauptnavigation steht links. Der Reiter **ViCo** besitzt eine zweite, thematisch gruppierte Navigation. Am unteren Fensterrand kann das **Diagnoseprotokoll** aufgeklappt werden. Lange Tabellen sind virtualisiert; Sortieren, Auswählen und Scrollen laden nicht sämtliche Zeilen gleichzeitig in die Oberfläche.

### Project Settings

Hier wird die Verbindung zu einem FEE-/Simulations-PC hergestellt.

1. PC im Dropdown auswählen. `localhost` ist für lokale Arbeit immer enthalten.
2. Gewünschte Ladeoptionen einstellen.
3. **Connect** wählen.
4. Statusmeldung und bei Fehlern das Diagnoseprotokoll prüfen.

Das Dropdown und die ViCo-PC-Suche verwenden dasselbe `WorkstationDirectory`. Die PC-/Benutzer-Zuordnung wird aus dem aktuellen Kanbanize-Cache übernommen; es gibt keine fest kompilierte Zuordnung. Deshalb kann ein PC erst nach einer Aktualisierung erscheinen, wenn er neu in Kanbanize angelegt wurde.

## ViCo

### Übersicht & Verbindung – PC-/Projektsuche

Die Suche besitzt zwei Modi:

- **Projekt** findet zu einer Projektnummer die zugehörigen PCs und Kanbanize-Informationen.
- **PC** filtert direkt nach PC-Name, Anzeigename, Benutzer oder zugeordnetem Projekt.

Die wichtigsten Spalten sind:

| Anzeige | Bedeutung |
|---|---|
| Status | `[B]` Backlog, `[P]` Planning, `[W]` Working, `[D]` Done |
| PC / Benutzer | Remote-PC und der aus Kanbanize ermittelte Benutzer |
| Projekte | Bis zu drei Projekte direkt; weitere werden zusammengefasst |
| Software | TIA Portal, Beckhoff TwinCAT und/oder Rockwell Studio 5000 |
| FEE / Hardware | Angaben der PC-Karte zu FEE, LAN und Hardware |
| Roboter | Anzahl eindeutig zugeordneter Software-Robotik-Karten; Details im Tooltip |
| Online | Zwischengespeichertes Ergebnis eines ICMP-Pings, keine Aussage über angemeldete Benutzer |

`installiert` wird nur angezeigt, wenn die Quellkarte die Installation ausdrücklich nennt. Sonst steht `laut Kanbanize angegeben`.

Nach Auswahl einer Ergebniszeile und – bei mehreren Treffern – eines Projekts stehen die passenden Aktionen zur Verfügung:

- Remote Desktop starten;
- TeamViewer starten;
- Projektablage des PCs öffnen;
- Simulationsprojekt öffnen;
- SPS-/Inbetriebnahmeprojekt öffnen;
- Planungsordner öffnen.

Für Remote Desktop hat der Benutzer der Kanbanize-Karte Vorrang. Die Anwendung erzeugt ein temporäres RDP-Profil mit den gewählten Monitoren und nutzt den kompatiblen Anmeldeablauf des bisherigen ViCo-Tools.

### Übersicht & Verbindung – Projekte & Favoriten

Dieser Bereich durchsucht die Simulationsprojektablage, öffnet ein Projektverzeichnis und verwaltet die mit dem bisherigen ViCo-Format kompatible Favoritenliste. **Speichern** speichert die Favoritenliste, nicht das ausgewählte Projekt selbst.

### Transfer

Der Transfer kopiert ausgewählte Projektbestandteile zwischen Quell- und Zielverzeichnis. Kopiervorgänge sind asynchron und ihre Parallelität ist begrenzt, damit Oberfläche, Datenträger und Netzlaufwerke ansprechbar bleiben. Vor dem Start Quelle, Ziel und Auswahl kontrollieren; vorhandene Dateien werden nach der im Dialog angezeigten Strategie behandelt.

### TIA Portal

TIA-Funktionen laufen bewusst in einem separaten Bridge-Prozess, weil TIA Openness versionsgebundene .NET-Framework-Abhängigkeiten verwendet.

Typischer Ablauf:

1. lokal installierte TIA-Version auswählen;
2. TIA-Projekt verbinden bzw. öffnen;
3. Programmbausteine und Datentypen importieren oder exportieren;
4. optional Achsbibliotheksablauf ausführen;
5. Status und Diagnoseprotokoll prüfen.

Die Bridge speichert Änderungen erst über die dafür vorgesehene Aktion. Ein nicht gestartetes TIA Portal, eine fehlende Openness-Berechtigung oder eine nicht passende Version wird als Fehler an die Hauptanwendung zurückgegeben.

### Verwaltung

Die Verwaltung zeigt:

- aktuellen Windows-Benutzer und erkanntes Lizenzlevel;
- heutige Outlook-Termine;
- verfügbare ViCo-Version;
- vorhandene und angefragte Lizenzeinträge.

Lizenzlevel ab Level8 dürfen Einträge bearbeiten. Eine Änderung wird nur gespeichert, wenn danach mindestens zwei unterschiedliche Windows-Benutzer Level9 besitzen. Falls eine Herabstufung diese Regel verletzen würde, im Feld **Zusätzlicher Level9-Benutzer** zuerst den Ersatz auswählen. Die Hochstufung wird vor der Herabstufung geschrieben.

## Bestehende VIBN-Werkzeuge

| Reiter | Aufgabe |
|---|---|
| CAD Wizard | CAD-Daten und Zuordnungen für das Simulationsmodell aufbereiten |
| Zuli Converter | Eingabedaten über formatabhängige Konvertierungsstrategien umwandeln |
| Container Generation | FEE-Container, Strukturen und optional KI-gestützte Zuordnungen erzeugen |
| Container2Fee | Containerinformationen in FEE-Objekte bzw. FEE-Strukturen übertragen |
| Special Devices | Spezielle Geräte über Katalog und Geräte-Factory erzeugen/konfigurieren |
| Model Validation | Modellregeln und FEE-Strukturen prüfen und Befunde anzeigen |
| Model Control | Achsen, Objekte, Roboter und Bewegungsabläufe im Modell steuern |
| Interface Operation | Schnittstellenoperationen zwischen Simulationskomponenten ausführen |
| AI-Test | Trainings-/Testabläufe für die KI-gestützte Containerzuordnung erproben |

Die genaue Verfügbarkeit einzelner Schaltflächen hängt vom verbundenen FEE-Projekt, ausgewählten Objekt, installierten SDK und aktuellen Arbeitszustand ab.

## Diagnoseprotokoll und Fehlersuche

Das Diagnoseprotokoll ist von jedem Reiter aus erreichbar. Es zeigt die letzten 500 Einträge und bietet Kopieren, Leeren und Öffnen des Logordners. Dateiprotokolle liegen unter `%LOCALAPPDATA%\GROB\VIBN_Tools\Logs` und rotieren täglich.

Bei einem Fehler immer festhalten:

1. Zeitpunkt und Reiter;
2. ausgeführte Aktion und Auswahl;
3. sichtbare Statusmeldung;
4. passenden Protokolleintrag;
5. bei TIA zusätzlich TIA-Version und Bridge-Status.

Kennwörter, API- und Lizenzschlüssel dürfen weder in Screenshots noch in Tickets oder Protokolle kopiert werden.
