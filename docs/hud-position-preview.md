# HUD-Positionen ohne andere Mods testen

Der optionale Testmodus erzeugt farbige Platzhalter für GK2RecipePin und Kebo Recipe Pins. Er nutzt dieselbe Prüfung der Bildschirmgrenzen, Sichtbarkeit und horizontalen Überlappung wie die echten Integrationen. Die Panels liegen auf einem separaten Canvas; ihre Koordinaten werden in den Canvas des Spiel-HUDs umgerechnet.

## Einschalten

1. Die neu gebaute `bin/Release/net472/GK2KnownFormulaHelper.dll` anstelle der bisher installierten Helper-DLL verwenden.
2. In `BepInEx/config/w00dst0ckOos.GK2LaboratoryFolioHelper.cfg` diesen Abschnitt setzen (eine vorhandene Sektion bearbeiten):

   ```ini
   [Debug.HudPositionPreview]
   Enabled = true
   ```

   Nach dem ersten Start mit der neuen DLL wird der Abschnitt automatisch angelegt. Standardmäßig ist der Testmodus ausgeschaltet.
3. Spiel neu starten, einen Spielstand laden und **F7** drücken.

**F7** schaltet zum nächsten Fall, **F6** zum vorherigen. Die Tasten lassen sich über `NextScenario` und `PreviousScenario` in derselben Konfigurationssektion ändern. Unten links stehen der aktuelle Fall und die Tasten.

Wenn keine echte Formel angeheftet ist, erscheint eine Karte **Position test / Preview only**. Sie wird nur für die Anzeige erzeugt und weder gespeichert noch in die Liste angehefteter Formeln aufgenommen. Bei vorhandenen Formel-Pins werden diese verwendet.

## Prüffälle

| Fall | Test | Erwartung |
| --- | --- | --- |
| 0 | Off | Normales HUD, keine Testkarte oder simulierten Pin-Panels. |
| 1 | Baseline | Formelkarte an ihrer normalen Position. |
| 2 | GK2RecipePin klein | Formelkarte unter dem blauen Panel, mit 12 Bildschirmpixeln Abstand. |
| 3 | GK2RecipePin groß | Formelkarte wandert entsprechend weiter nach unten. |
| 4 | Kebo rechts | Formelkarte unter dem orangefarbenen Panel. |
| 5 | Kebo links | Formelkarte wieder an der normalen Position. |
| 6 | Kebo mittig | Verschiebung nur, wenn das Panel horizontal in den Bereich der Formelkarte hineinragt. |
| 7 | Beide Panels | Formelkarte unter der tiefsten Unterkante der überlappenden Panels. |
| 8 | Kebo transparent | Panel hat CanvasGroup-Alpha 0 und reserviert keinen Platz. |
| 9 | Kebo inaktiv | Deaktiviertes Panel reserviert keinen Platz. |

Vorwärts und rückwärts durchschalten, besonders zwischen 3 und 2 sowie zwischen 4 und 5. Die Formelkarte soll auch wieder nach oben zurückkehren und ihre Größe behalten. Zusätzlich verschiedene Spielauflösungen und UI-Skalierungen prüfen. Sehr große externe Listen können weiterhin den Platz unterhalb der Panels aufbrauchen; die Vorschau macht diesen Fall sichtbar.

Bereits installierte echte HUD-Mods werden weiter berücksichtigt und können die Ausgangsposition beeinflussen. Die Test-Panels bilden typische Größen und Positionen ab, nicht das vollständige Aussehen oder jede Einstellung der Originalmods. Loader und Harmony-Hooks werden mit dieser Vorschau nicht getestet.

## Ausschalten

Für die laufende Sitzung zu **0 / Off** wechseln. Zum vollständigen Abschalten `Enabled = false` setzen und das Spiel neu starten.
