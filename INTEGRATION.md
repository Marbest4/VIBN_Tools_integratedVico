# ViCo-, Kanbanize- und TIA-Integration

Die Integration ist modular aufgebaut:

- Die bestehende VIBN-Tools-WPF-Oberfläche bleibt der Host.
- ViCo nutzt Core-Modelle und Infrastrukturadapter statt übernommener Großklassen.
- Kanbanize trennt manuelle Karten von der idempotenten VIBN-zu-Arbeitsplätze-Synchronisierung.
- Die TIA-Bridge kapselt Siemens Openness in einem separaten Prozess.
- Rollen ersetzen die frühere Lizenzlogik.

Die aktuelle Architektur, alle Funktionsbereiche, Erweiterungspunkte und Tests sind in [docs/GESAMTLOESUNG.md](docs/GESAMTLOESUNG.md), [docs/ENTWICKLERHANDBUCH.md](docs/ENTWICKLERHANDBUCH.md) und [docs/BENUTZERHANDBUCH.md](docs/BENUTZERHANDBUCH.md) dokumentiert.
