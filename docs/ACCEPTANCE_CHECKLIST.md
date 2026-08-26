# Release-Abnahmecheckliste

Diese Liste auf einem GROB-Desktop mit Netzwerkzugriff, FEE, Kanbanize-Berechtigung und mindestens einer unterstützten TIA-Installation ausführen.

## Automatische Basis

- [ ] `dotnet build VIBN_Tools_App.sln --configuration Release` hat keine Fehler.
- [ ] `Tests/CoreSmokeTests` ist erfolgreich.
- [ ] `Tests/UiStartupSmokeTests` ist erfolgreich und meldet keine Binding-Fehler.
- [ ] Anwendung startet ohne XamlParseException.

## Rollen und Navigation

- [ ] Nicht-Level7-Benutzer sehen CAD Wizard, Container Generation und Container2Fee nicht.
- [ ] Level7 sieht genau diese drei Bereiche zusätzlich.
- [ ] Level8 sieht außerdem Kanbanize Karten, AI-Test und ViCo-Verwaltung.
- [ ] Level9 kann Rollen ändern; Level8 kann sie nur ansehen.
- [ ] `lutzma` wird als Level9 erkannt und kann nicht verändert/entfernt werden.
- [ ] Eine Änderung, die weniger als zwei Level9-Benutzer hinterließe, wird abgewiesen.

## Project Settings und ViCo

- [ ] Project Settings zeigt nur erreichbare PCs und der Filter wirkt sofort.
- [ ] Ein fehlgeschlagener FEE-Connect zeigt nicht fälschlich „verbunden“.
- [ ] ViCo-Suche findet PC, Benutzer und Projekt mit demselben Suchfeld.
- [ ] Spalten Belegung, Software, Standort, Projekt-IP, Sonstiges, RDP-Sitzung, letzte Anmeldung und Benutzer sind plausibel.
- [ ] Nur Planung/In-Arbeit-Projekte stehen in der aktiven Projektauswahl; Backlog/Abschluss stehen im Detailbereich.
- [ ] Frei ist grün, Belegt rot; Online ist grün, Offline rot.
- [ ] Offline-PCs zeigen keine Remote-/Pfadbuttons.
- [ ] RDP-Sitzungsrechte fehlen: Anzeige lautet „Nicht abrufbar“, nicht „offline“.
- [ ] Automatischer Remote-Button nutzt den Kanbanize-Benutzer; der zweite Button zeigt den Windows-Anmeldedialog.
- [ ] Eine vorhandene KONFIGURATION-Unteraufgabe lässt sich bearbeiten und zurückspeichern; keine andere Karteninformation ändert sich.

## Kanbanize

- [ ] Vorschau verwendet Quell- und Zielboard, Lane und Spalte korrekt.
- [ ] Start ist Quell-Deadline minus 14 Tage.
- [ ] Ziel-Deadline ist Quell-Deadline plus 56 Tage.
- [ ] Nur in der Vorschau markierte Sync-Zeilen werden erstellt oder aktualisiert.
- [ ] Ein zweiter Lauf erzeugt keine Duplikate.
- [ ] Mehrdeutige Zielkarte führt zu Konflikt ohne Änderung.
- [ ] Bestehende generierte Karte ändert nur Startfeld und Deadline, nicht Titel/Position/Beschreibung.
- [ ] Eigene Karte kann unabhängig erstellt werden.

## TIA und Special Devices

- [ ] TIA-Version, Attach und PLC-Auswahl funktionieren.
- [ ] Hardwareansicht zeigt Gerätename, Modul, Slot, Modultyp/Typkennung, optionale Firmware, E-/A-Byte und Byte-Längen.
- [ ] Special-Device-Hardwaretabelle übernimmt nur bewusst ausgewählte/validierte Zeilen.
- [ ] Geräte erscheinen zuerst in der Warteschlange.
- [ ] Fehlerhafte FEE-Erzeugung bleibt prüfbar in der Warteschlange.

## Bestehende VIBN-Funktionen

- [ ] CAD Wizard, Zuli Converter, Container Generation und Container2Fee funktionieren mit einer bekannten Testvorlage.
- [ ] Model Validation, Model Control und Interface Operation funktionieren mit dem Testmodell.
- [ ] Keine bestehende Funktion wurde durch ViCo-/Kanbanize-Aufrufe verändert.

## Übergabe

- [ ] Diagnoseprotokoll enthält keine sensiblen Werte.
- [ ] Anwenderhandbuch und Screenshots sind Bestandteil des Releasepakets.
- [ ] Bekannte externe SDK-Warnungen bzw. Abhängigkeiten sind dokumentiert und keine neue funktionale Warnung aus den geänderten Integrationsmodulen offen.
