# Englische Übersetzung – Leitfaden

## Technik
- Der Code bleibt deutsch. Übersetzt wird **zur Anzeigezeit** (`Scripts/Core/Localization/Lang.cs`):
  - UI Toolkit: `Runtime/UI/LocSweeper.cs` übersetzt jeden sichtbaren `Label`/`Button` automatisch.
  - 3D-Text: `Label3D.SetText` übersetzt automatisch.
  - Sonstiges (z. B. `Debug`-freie Strings in eigenen Mesh-Texten): `Lang.T(text)` bzw. `Loc.Tr(text)` aufrufen.
- Wörterbücher: `Scripts/Core/Localization/En*.cs`, Einträge `A("Deutsch", "English");`
  - **exakt**: der komplette angezeigte Text.
  - **Vorlage**: Platzhalter `{0}`, `{1}` … für alles, was zur Laufzeit eingesetzt wird (Zahlen, Beträge, Namen, Produkte). Eingesetzte Teile werden selbst wieder übersetzt (Produktname in einer Meldung → englischer Produktname). Beispiel: `A("Lieferung angekommen: {0}× {1} am Wareneingang.", "Delivery arrived: {0}× {1} at goods-in.");`
  - Mehrzeilige Texte werden auch zeilenweise übersetzt, wenn der ganze Text nicht als Eintrag existiert – Mails trotzdem möglichst als Ganzes (oder Absatz für Absatz) eintragen.
  - Vorlagen müssen den deutschen Text **exakt** treffen (Leerzeichen, Satzzeichen, „×“, „€“). Bei Verkettungen im Code (`"A " + x + " B"`) die Vorlage `"A {0} B"` eintragen.
  - Wenn ein Text aus vielen dynamischen Stücken zusammengesetzt wird und keine sinnvolle Vorlage möglich ist, darf der Code minimal umgebaut werden (z. B. `string.Format` mit fester Vorlage) – Verhalten im Deutschen unverändert.
- Fehlende Texte sammelt `Lang.Missing` (bei aktivem Englisch).

## Stil
- Englisch locker, frech, satirisch – wie das deutsche Original (Hustle-Kultur-Satire). Keine wörtliche Übersetzung von Witzen: lieber einen gleichwertigen englischen Witz.
- Parodie-Marken bleiben (AllesExpress → **AllesExpress** bleibt als „deutsche Marke“? Nein: international verständlich machen): AllesExpress → AllExpress? → **AliBiBi**? – Entscheidung: Marken mit deutschem Wortwitz bekommen englische Parodienamen, sonst bleiben sie:
  - AllesExpress → **EverythingXpress**, Mein Shop → **My Shop**, iWolke → **iCloudy**, PaketBlitz → **ParcelBolt**, TradingViech → **TradingBeast**, Revoluut (bleibt), TikTak (bleibt), Fakebook (bleibt), Gugel (bleibt), Kramkiste → **JunkBox**, MediaMarkd → **MediaMarkt-parody: „MegaMarked“**, Fressnix → **Chewnothing**, Kalles Imbiss → **Kalle's Diner**, Stadtblatt → **City Gazette**, Kiezbank → **Block Bank**, Spedition/Firmen nach Gefühl.
- Währung bleibt €. Zahlenformat in Englisch: `1,234.50 €` (Fmt passt das an).
- Wochentage, Uhrzeit: 24-h-Format bleibt.
