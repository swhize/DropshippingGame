namespace DropshippingGame.Core
{
    /// <summary>Eine Erklär-Nachricht der ersten Spielstunden (Marvin, Hustle-Akademie).</summary>
    public sealed class OnboardingTip
    {
        /// <summary>Schlüssel in <see cref="Sim.Explained"/> (einmal pro Spielstand).</summary>
        public string Key;
        public string Icon = "chat";
        public string Title, Text;
    }

    /// <summary>
    /// Onboarding nach dem Tutorial: kurze Nachrichten, die neue Systeme erst dann erklären, wenn sie
    /// relevant bzw. freigeschaltet werden (Reihenfolge = Priorität). Die Bedingungen stehen in
    /// <c>Sim.Onboarding.cs</c>. Platzhalter <c>{key:aktion}</c> werden über <see cref="FillKeys"/> ersetzt.
    /// </summary>
    public static partial class GameData
    {
        /// <summary>Mindestabstand zwischen zwei Onboarding-Nachrichten (Geschäftsminuten).</summary>
        public const float OnboardingGapMinutes = 45f;

        public static readonly OnboardingTip[] OnboardingTips =
        {
            new OnboardingTip
            {
                Key = "post", Icon = "truck", Title = "Versand: gratis oder bequem",
                Text = "Die Abholbox am Garagentor kostet 3 € pro Paket. Packstationen in der Stadt und der Schalter in der PaketBlitz-Filiale " +
                       "nehmen gratis an. Tipp: Mehrere Pakete sammeln und zusammen wegbringen – der Kompass oben zeigt die nächste Packstation.",
            },
            new OnboardingTip
            {
                Key = "phone", Icon = "phone", Title = "Dein Handy",
                Text = "Mit {key:phone} öffnest du dein Handy, ohne die Arbeit zu unterbrechen: Bestellzettel, Nachrichten, Nachbestellen mit " +
                       "einem Tipp und Kontostand. Den Laptop brauchst du für Einkauf, Shop und Firma.",
            },
            new OnboardingTip
            {
                Key = "dayend", Icon = "moon", Title = "Feierabend um 20 Uhr",
                Text = "Um 20 Uhr ist Schluss: Dann werden Miete und Löhne fällig und du siehst deine Tagesbilanz. Früher fertig? " +
                       "Leg dich auf die Matratze in der Garage. Über Nacht erholt sich deine Energie.",
            },
            new OnboardingTip
            {
                Key = "energy", Icon = "battery", Title = "Deine Energie",
                Text = "Unter 40 Energie wirst du langsamer. Schlafen füllt sie über Nacht auf. Besseres Bett, Bürostuhl und Kaffee " +
                       "gibt's im Laptop unter Firma › Einrichtung.",
            },
            new OnboardingTip
            {
                Key = "waste", Icon = "boxes", Title = "Verpackungsmüll",
                Text = "Jedes Paket und jede Kiste macht Müll. Die Müllabfuhr kommt Mo, Mi und Fr um 10 Uhr. Sind die Tonnen voll, " +
                       "arbeitet ihr langsamer und die Nachbarn meckern. Abhilfe: Sonderabholung oder Großcontainer (Firma › Einrichtung).",
            },
            new OnboardingTip
            {
                Key = "porto", Icon = "receipt", Title = "Porto & Paketdienste",
                Text = "Jedes Paket kostet etwas Porto. Unter Firma › Logistik wählst du den Paketdienst: PaketBlitz ist günstig, " +
                       "TurboKurier doppelt so teuer, aber immer pünktlich – praktisch bei Streik.",
            },
            new OnboardingTip
            {
                Key = "city", Icon = "globe", Title = "Die Stadt erkunden",
                Text = "Im Westen liegt das Einkaufsviertel mit MediaMarkd (Technik) und Fressnix (Haustiere), im Osten PaketBlitz-Filiale, " +
                       "Bank und Börse. Der Kompass oben zeigt dir die Richtung (in den Einstellungen abschaltbar).",
            },
            new OnboardingTip
            {
                Key = "garage", Icon = "home", Title = "Garage einrichten & Emotes",
                Text = "Möbel stehen im Weg? Anvisieren, {key:build} drücken, verschieben und drehen. Für Videos und Spaß: " +
                       "{key:emote} gedrückt halten öffnet das Emote-Rad.",
            },
            new OnboardingTip
            {
                Key = "money_low", Icon = "bank", Title = "Knapp bei Kasse?",
                Text = "Dein Konto ist fast leer. Fällt es abends unter das Limit, droht die Pleite. Hilfe: Kredit in der Revoluut-App, " +
                       "kleinere Mengen einkaufen oder Preise im Shop prüfen.",
            },
            new OnboardingTip
            {
                Key = "tiktok", Icon = "music", Title = "TikToks drehen",
                Text = "Ab jetzt kannst du TikToks drehen: Ringlicht am Schreibtisch, Handy oder Laptop › TikTak. Produkt mittig, nah und " +
                       "ruhig filmen, gleich am Anfang eine Aktion ({key:rec_action}). Gute Videos bringen tagelang mehr Kundschaft.",
            },
            new OnboardingTip
            {
                Key = "ads", Icon = "thumb", Title = "Werbung schalten",
                Text = "Fakebook-Anzeigen sind freigeschaltet: Sie bringen gezielt mehr Bestellungen, kosten aber pro Tag. " +
                       "Erst schalten, wenn genug Ware im Regal liegt!",
            },
            new OnboardingTip
            {
                Key = "finance", Icon = "trend", Title = "Bank & Börse",
                Text = "Geld übrig? In der Revoluut-App gibt's das Sparkonto, in TradingViech und an der Börse im Finanzviertel Aktien, " +
                       "ETFs und Krypto. Kurse schwanken – setz nie dein Einkaufsbudget aufs Spiel.",
            },
            new OnboardingTip
            {
                Key = "warehouse", Icon = "building", Title = "Zeit für mehr Platz",
                Text = "Die Lagerhalle ist kaufbar (Firma › Ausbau): mehr Regale, später Personal und Förderband. " +
                       "Die Miete ist höher – rechne vorher!",
            },
            new OnboardingTip
            {
                Key = "staff", Icon = "users", Title = "Personal einstellen",
                Text = "Unter Firma › Team stellst du Leute ein: Sie holen, packen und versenden automatisch. Löhne werden jeden Abend fällig.",
            },
        };
    }
}
