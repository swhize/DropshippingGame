# Assets – Katalog und AssetLib

Alle Drittanbieter-Assets liegen unter `Assets/DropshippingGame/Resources/` und werden über `DropshippingGame.AssetLib` (`Scripts/Runtime/Rendering/AssetLib.cs`) geladen. Quellen/Lizenzen: `CREDITS.md`, `ThirdParty/Licenses/`, `Resources/credits.json`. Vorschaubilder: `docs/asset-previews/` (roter Kegel = Unity +Z = Front).

## So funktioniert AssetLib

- `AssetLib.Model(id, parent, localPos, rotY, fit, collider, castShadows)` instanziert das FBX aus dem Manifest, dreht es per `forwardRotY`, sodass die Front nach lokal **+Z** zeigt, skaliert es uniform auf `meters` entlang `fitAxis` (bzw. auf `fit`, wenn > 0), setzt die Unterkante auf y = 0 und zentriert X/Z. Optional BoxCollider. Nicht-URP-Materialien werden zu `Universal Render Pipeline/Lit` umgebaut (Textur aus dem Manifest-Feld `texture`). Gibt `null` zurück, wenn etwas fehlt – nie Exceptions.
- `HasModel`, `Info`, `Ids(category)`, `Categories()`, `ModelBounds(id)` (Größe nach Skalierung, ohne Instanz), `Tint(go, color)` (z. B. Lieferwagen gelb).
- `Surface(id, tileMeters)` baut ein URP-Lit-Material mit Base/Normal/Smoothness(Alpha der *_roughness.png)/AO; UV-Skala = 1/Kachelgröße (wie `Mats`). `tileMeters <= 0` nimmt die empfohlene Größe.
- `Sfx(name)` wählt zufällig `name`, `name_2`, … ; `Music(name)`, `Ambience(name)`, `Font("ui"|"ui_bold"|"display"|"mono"|"mono_bold")`, `Hdri("day"|"sunset"|"night")`, `Texture(path)`.
- Charaktere: **Legacy-Animation** (Import-Regel setzt `ModelImporterAnimationType.Legacy`) – kein AnimatorController nötig. `AssetLib.PlayAnim(go, "walk")` / `"idle"` blendet per `Animation.CrossFade`. Loopende Clips: idle, walk, sprint, sit, drive, holding-*. Clip-Namen werden beim Import auf den Kurznamen gekürzt; PlayAnim findet notfalls auch `root|walk|…`.
- `Scripts/Editor/AssetImportRules.cs`: Modelle ohne Kameras/Lichter, Material per MaterialDescription als URP/Lit opak; Normalmaps als NormalMap, Roughness/AO linear; Musik/Ambience Streaming, SFX DecompressOnLoad; HDRIs als Cubemap.

## Orientierung & Maße

Alle Modelle wurden in Blender mit Unitys FBX-Achsen gerendert und vermessen: **alle zeigen mit der Front nach +Z** (`forwardRotY = 0`, hohe Sicherheit für Fahrzeuge/Charaktere/Möbel mit klarer Front; bei symmetrischen Objekten wie Bäumen, Kisten egal). Pivot: Boden (AssetLib setzt ohnehin Unterkante auf 0). `meters` = größte Ausdehnung in Metern (Charaktere: Höhe, `fitAxis: y`), geschätzt über einen Skalenfaktor pro Pack – bei Bedarf per `fit` überschreiben. Achtung: die alten prozeduralen `Props.Car/Van` zeigen nach +X → beim Ersetzen `rotY + 90`.

## Empfehlungen für die Welt

| Stelle | ID |
|---|---|
| Lieferwagen (VanStop) | `vehicle.van` (gelb tinten) |
| Spieler-Kombi (Garage, Decor `auto`) | `vehicle.kombi` |
| Sportwagen (Decor `sportwagen`) | `vehicle.sportscar` |
| Verkehr | `vehicle.traffic_1..5`, `vehicle.police`, `vehicle.car_police` |
| Skyline (z=±42) | `city.building_skyscraper_a..e`, `city.low_detail_building_*`; Vordergrund `city.building_a..h` (KayKit) |
| Straßenlaternen | `street.streetlight`, `street.streetlight_old_single` |
| Bäume/Büsche | `nature.tree_a..e`, `nature.bush_a..c` |
| Lager | `logistics.pallet`, `logistics.box`, `logistics.conveyor*`, `logistics.scanner_low`, `logistics.locker`, `logistics.workbench` |
| Diner | `diner.stove_multi`, `diner.fridge_a`, `diner.kitchencounter_*`, `diner.table_round_b_tablecloth_red`, `diner.chair_a`, `diner.chair_stool`, `food.food_burger` |
| Decor-Slots | pflanze `decor.plant_1`, poster `decor.poster`, stehlampe `decor.floor_lamp`, teppich `decor.rug`, billy `decor.shelf`, sofa `decor.sofa`, gamingstuhl `decor.gaming_chair` |
| Personal | `character.worker_1` (male_e, Weste), `character.worker_2` (female_f, Weste), Koch Kalle `character.cook` (male_b); Passanten/Kunden: übrige `character.*` |

## Oberflächen

| ID | Kachel (m) | Maps |
|---|---|---|
| `asphalt` | 3.0×3.0 | basecolor, normal, roughness |
| `sidewalk` | 1.0×1.0 | basecolor, normal, roughness |
| `grass` | 2.0×2.0 | ao, basecolor, normal, roughness |
| `concrete_floor` | 3.0×3.0 | basecolor, normal, roughness |
| `concrete_wall` | 2.5×2.5 | ao, basecolor, normal, roughness |
| `block_wall` | 1.2×1.2 | ao, basecolor, normal, roughness |
| `brick_wall` | 2.0×1.0 | ao, basecolor, normal, roughness |
| `plaster_wall` | 2.0×2.0 | basecolor, normal, roughness |
| `wood_floor` | 1.0×1.0 | ao, basecolor, normal, roughness |
| `tile_floor` | 1.2×1.2 | basecolor, normal, roughness |
| `tile_wall` | 1.2×1.2 | ao, basecolor, normal, roughness |
| `metal_sheet` | 1.5×1.5 | ao, basecolor, normal, roughness |
| `roof` | 2.0×1.0 | ao, basecolor, normal, roughness |
| `shutter` | 1.5×1.5 | basecolor, normal, roughness |

## Audio

- SFX: `bad`, `box_close`, `box_open`, `cash`, `click`, `coin`, `door`, `door_close`, `drop`, `error`, `fold`, `hover`, `jingle_bad`, `jingle_good`, `latch`, `levelup`, `metal_clank`, `notify`, `order`, `pallet`, `phone_close`, `phone_open`, `pickup`, `place`, `plate`, `printer`, `register`, `scanner`, `stamp`, `step`, `step_asphalt`, `step_concrete`, `step_grass`, `step_tile`, `step_wood`, `tape`, `typing`, `whoosh`
- Ambience: 
- Musik: `menu`, `work_1`, `work_2`, `work_3`, `diner`, `diner_2`, `evening`
- Kein `truck`/`forklift_beep` (keine passende freie Quelle) → Synth-Fallback.

## Modelle

| ID | Meter | Achse | Clips / Beschreibung | Pfad |
|---|---|---|---|---|
| `character.cook` | 1.8 | y | idle, walk | Models/kenney_mini_characters/character-male-b |
| `character.female_a` | 1.94 | y | idle, walk | Models/kenney_mini_characters/character-female-a |
| `character.female_b` | 1.81 | y | idle, walk | Models/kenney_mini_characters/character-female-b |
| `character.female_c` | 1.94 | y | idle, walk | Models/kenney_mini_characters/character-female-c |
| `character.female_d` | 1.94 | y | idle, walk | Models/kenney_mini_characters/character-female-d |
| `character.female_e` | 1.79 | y | idle, walk | Models/kenney_mini_characters/character-female-e |
| `character.female_f` | 1.68 | y | idle, walk | Models/kenney_mini_characters/character-female-f |
| `character.male_a` | 1.68 | y | idle, walk | Models/kenney_mini_characters/character-male-a |
| `character.male_b` | 1.65 | y | idle, walk | Models/kenney_mini_characters/character-male-b |
| `character.male_c` | 1.98 | y | idle, walk | Models/kenney_mini_characters/character-male-c |
| `character.male_d` | 1.8 | y | idle, walk | Models/kenney_mini_characters/character-male-d |
| `character.male_e` | 1.69 | y | idle, walk | Models/kenney_mini_characters/character-male-e |
| `character.male_f` | 1.68 | y | idle, walk | Models/kenney_mini_characters/character-male-f |
| `character.worker_1` | 1.8 | y | idle, walk | Models/kenney_mini_characters/character-male-e |
| `character.worker_2` | 1.7 | y | idle, walk | Models/kenney_mini_characters/character-female-f |
| `city.building_a` | 9.4 | max |  | Models/kaykit_city/building_A |
| `city.building_a_2` | 9.7 | max |  | Models/kenney_city_commercial/building-a |
| `city.building_b` | 9.4 | max |  | Models/kaykit_city/building_B |
| `city.building_b_2` | 9.7 | max |  | Models/kenney_city_commercial/building-b |
| `city.building_c` | 13.99 | max |  | Models/kaykit_city/building_C |
| `city.building_c_2` | 8.18 | max |  | Models/kenney_city_commercial/building-c |
| `city.building_d` | 13.96 | max |  | Models/kaykit_city/building_D |
| `city.building_d_2` | 9.7 | max |  | Models/kenney_city_commercial/building-d |
| `city.building_e` | 11.05 | max |  | Models/kaykit_city/building_E |
| `city.building_e_2` | 12.3 | max |  | Models/kenney_city_commercial/building-e |
| `city.building_f` | 11.05 | max |  | Models/kaykit_city/building_F |
| `city.building_f_2` | 12.7 | max |  | Models/kenney_city_commercial/building-f |
| `city.building_g` | 13.99 | max |  | Models/kaykit_city/building_G |
| `city.building_g_2` | 12.7 | max |  | Models/kenney_city_commercial/building-g |
| `city.building_h` | 14.33 | max |  | Models/kaykit_city/building_H |
| `city.building_h_2` | 9.7 | max |  | Models/kenney_city_commercial/building-h |
| `city.building_i` | 12.6 | max |  | Models/kenney_city_commercial/building-i |
| `city.building_j` | 15.63 | max |  | Models/kenney_city_commercial/building-j |
| `city.building_k` | 15.63 | max |  | Models/kenney_city_commercial/building-k |
| `city.building_l` | 17.02 | max |  | Models/kenney_city_commercial/building-l |
| `city.building_m` | 23.62 | max |  | Models/kenney_city_commercial/building-m |
| `city.building_n` | 18.6 | max |  | Models/kenney_city_commercial/building-n |
| `city.building_skyscraper_a` | 21.6 | max |  | Models/kenney_city_commercial/building-skyscraper-a |
| `city.building_skyscraper_b` | 33.6 | max |  | Models/kenney_city_commercial/building-skyscraper-b |
| `city.building_skyscraper_c` | 30.6 | max |  | Models/kenney_city_commercial/building-skyscraper-c |
| `city.building_skyscraper_d` | 41.02 | max |  | Models/kenney_city_commercial/building-skyscraper-d |
| `city.building_skyscraper_e` | 30.6 | max |  | Models/kenney_city_commercial/building-skyscraper-e |
| `city.low_detail_building_a` | 15.0 | max |  | Models/kenney_city_commercial/low-detail-building-a |
| `city.low_detail_building_b` | 16.69 | max |  | Models/kenney_city_commercial/low-detail-building-b |
| `city.low_detail_building_c` | 16.88 | max |  | Models/kenney_city_commercial/low-detail-building-c |
| `city.low_detail_building_d` | 13.12 | max |  | Models/kenney_city_commercial/low-detail-building-d |
| `city.low_detail_building_e` | 13.5 | max |  | Models/kenney_city_commercial/low-detail-building-e |
| `city.low_detail_building_f` | 15.0 | max |  | Models/kenney_city_commercial/low-detail-building-f |
| `city.low_detail_building_g` | 15.0 | max |  | Models/kenney_city_commercial/low-detail-building-g |
| `city.low_detail_building_h` | 15.75 | max |  | Models/kenney_city_commercial/low-detail-building-h |
| `city.low_detail_building_i` | 13.31 | max |  | Models/kenney_city_commercial/low-detail-building-i |
| `city.low_detail_building_j` | 13.12 | max |  | Models/kenney_city_commercial/low-detail-building-j |
| `city.low_detail_building_k` | 11.62 | max |  | Models/kenney_city_commercial/low-detail-building-k |
| `city.low_detail_building_l` | 13.88 | max |  | Models/kenney_city_commercial/low-detail-building-l |
| `city.low_detail_building_m` | 14.81 | max |  | Models/kenney_city_commercial/low-detail-building-m |
| `city.low_detail_building_n` | 5.25 | max |  | Models/kenney_city_commercial/low-detail-building-n |
| `city.low_detail_building_wide_a` | 8.25 | max |  | Models/kenney_city_commercial/low-detail-building-wide-a |
| `city.low_detail_building_wide_b` | 8.62 | max |  | Models/kenney_city_commercial/low-detail-building-wide-b |
| `city.watertower` | 3.15 | max |  | Models/kaykit_city/watertower |
| `decor.book_set` | 0.55 | max |  | Models/kaykit_furniture/book_set |
| `decor.cactus_medium_a` | 0.62 | max |  | Models/kaykit_furniture/cactus_medium_A |
| `decor.cactus_small_a` | 0.39 | max |  | Models/kaykit_furniture/cactus_small_A |
| `decor.cup_pencils` | 0.47 | max |  | Models/kaykit_furniture/cup_pencils |
| `decor.floor_lamp` | 1.75 | max | Stehlampe | Models/kaykit_furniture/lamp_standing |
| `decor.gameconsole_handheld` | 0.73 | max |  | Models/kaykit_furniture/gameconsole_handheld |
| `decor.gaming_chair` | 1.2 | max | Gamingstuhl | Models/kaykit_furniture/chair_desk_B |
| `decor.lamp_desk` | 0.83 | max |  | Models/kaykit_furniture/lamp_desk |
| `decor.lamp_desk_headphones` | 0.83 | max |  | Models/kaykit_furniture/lamp_desk_headphones |
| `decor.lamp_standing` | 1.76 | max |  | Models/kaykit_furniture/lamp_standing |
| `decor.lamp_table` | 0.72 | max |  | Models/kaykit_furniture/lamp_table |
| `decor.mug_a` | 0.33 | max |  | Models/kaykit_furniture/mug_A |
| `decor.pictureframe_large_a` | 0.84 | max |  | Models/kaykit_furniture/pictureframe_large_A |
| `decor.pictureframe_large_b` | 1.4 | max |  | Models/kaykit_furniture/pictureframe_large_B |
| `decor.pictureframe_standing_a` | 0.43 | max |  | Models/kaykit_furniture/pictureframe_standing_A |
| `decor.plant_1` | 0.6 | max | Pflanze | Models/kaykit_furniture/cactus_medium_A |
| `decor.plant_2` | 0.4 | max | Pflanze klein | Models/kaykit_furniture/cactus_small_A |
| `decor.poster` | 0.9 | max | Bild/Poster (Wand) | Models/kaykit_furniture/pictureframe_large_A |
| `decor.rug` | 2.1 | max | Teppich | Models/kaykit_furniture/rug_rectangle_stripes_A |
| `decor.rug_oval_a` | 2.1 | max |  | Models/kaykit_furniture/rug_oval_A |
| `decor.rug_rectangle_a` | 2.1 | max |  | Models/kaykit_furniture/rug_rectangle_A |
| `decor.rug_rectangle_stripes_a` | 2.1 | max |  | Models/kaykit_furniture/rug_rectangle_stripes_A |
| `decor.shelf` | 1.4 | max | Regal (Billy) | Models/kaykit_furniture/cabinet_medium_decorated |
| `decor.sofa` | 2.1 | max | Sofa | Models/kaykit_furniture/couch_pillows |
| `diner.chair_a` | 1.03 | max |  | Models/kaykit_restaurant/chair_A |
| `diner.chair_b` | 1.03 | max |  | Models/kaykit_restaurant/chair_B |
| `diner.chair_stool` | 0.64 | max |  | Models/kaykit_restaurant/chair_stool |
| `diner.crate` | 1.7 | max |  | Models/kaykit_restaurant/crate |
| `diner.crate_lid` | 1.7 | max |  | Models/kaykit_restaurant/crate_lid |
| `diner.crate_potatoes` | 1.7 | max |  | Models/kaykit_restaurant/crate_potatoes |
| `diner.cuttingboard` | 1.27 | max |  | Models/kaykit_restaurant/cuttingboard |
| `diner.dishrack_plates` | 1.02 | max |  | Models/kaykit_restaurant/dishrack_plates |
| `diner.extractorhood` | 1.7 | max |  | Models/kaykit_restaurant/extractorhood |
| `diner.fridge_a` | 2.12 | max |  | Models/kaykit_restaurant/fridge_A |
| `diner.fridge_b` | 2.12 | max |  | Models/kaykit_restaurant/fridge_B |
| `diner.icecream_machine` | 2.04 | max |  | Models/kaykit_restaurant/icecream_machine |
| `diner.jar_a_large` | 0.64 | max |  | Models/kaykit_restaurant/jar_A_large |
| `diner.kitchencabinet` | 1.7 | max |  | Models/kaykit_restaurant/kitchencabinet |
| `diner.kitchencounter_outercorner` | 1.7 | max |  | Models/kaykit_restaurant/kitchencounter_outercorner |
| `diner.kitchencounter_sink` | 1.74 | max |  | Models/kaykit_restaurant/kitchencounter_sink |
| `diner.kitchencounter_straight_a` | 1.74 | max |  | Models/kaykit_restaurant/kitchencounter_straight_A |
| `diner.kitchencounter_straight_a_decorated` | 1.92 | max |  | Models/kaykit_restaurant/kitchencounter_straight_A_decorated |
| `diner.kitchencounter_straight_decorated` | 1.74 | max |  | Models/kaykit_restaurant/kitchencounter_straight_decorated |
| `diner.kitchentable_a` | 1.7 | max |  | Models/kaykit_restaurant/kitchentable_A |
| `diner.menu` | 0.68 | max |  | Models/kaykit_restaurant/menu |
| `diner.oven` | 2.0 | max |  | Models/kaykit_restaurant/oven |
| `diner.pan_a` | 1.27 | max |  | Models/kaykit_restaurant/pan_A |
| `diner.pizzabox_stacked` | 1.97 | max |  | Models/kaykit_restaurant/pizzabox_stacked |
| `diner.plate` | 0.81 | max |  | Models/kaykit_restaurant/plate |
| `diner.plate_small` | 0.64 | max |  | Models/kaykit_restaurant/plate_small |
| `diner.pot_a` | 1.19 | max |  | Models/kaykit_restaurant/pot_A |
| `diner.shelf_papertowel_decorated` | 1.7 | max |  | Models/kaykit_restaurant/shelf_papertowel_decorated |
| `diner.stove_multi` | 1.94 | max |  | Models/kaykit_restaurant/stove_multi |
| `diner.stove_multi_countertop` | 1.48 | max |  | Models/kaykit_restaurant/stove_multi_countertop |
| `diner.stove_single` | 1.94 | max |  | Models/kaykit_restaurant/stove_single |
| `diner.table_round_a` | 2.55 | max |  | Models/kaykit_restaurant/table_round_A |
| `diner.table_round_a_small` | 1.27 | max |  | Models/kaykit_restaurant/table_round_A_small |
| `diner.table_round_b_tablecloth_red` | 2.55 | max |  | Models/kaykit_restaurant/table_round_B_tablecloth_red |
| `diner.table_round_b_tablecloth_red_decorated` | 2.55 | max |  | Models/kaykit_restaurant/table_round_B_tablecloth_red_decorated |
| `diner.wall_orderwindow` | 3.4 | max |  | Models/kaykit_restaurant/wall_orderwindow |
| `food.bag` | 0.18 | max |  | Models/kenney_food/bag |
| `food.bottle_ketchup` | 0.12 | max |  | Models/kenney_food/bottle-ketchup |
| `food.bottle_musterd` | 0.12 | max |  | Models/kenney_food/bottle-musterd |
| `food.burger` | 0.12 | max |  | Models/kenney_food/burger |
| `food.burger_cheese` | 0.11 | max |  | Models/kenney_food/burger-cheese |
| `food.can` | 0.1 | max |  | Models/kenney_food/can |
| `food.carton` | 0.18 | max |  | Models/kenney_food/carton |
| `food.cup_coffee` | 0.09 | max |  | Models/kenney_food/cup-coffee |
| `food.cup_saucer` | 0.11 | max |  | Models/kenney_food/cup-saucer |
| `food.food_burger` | 0.81 | max |  | Models/kaykit_restaurant/food_burger |
| `food.food_dinner` | 0.88 | max |  | Models/kaykit_restaurant/food_dinner |
| `food.food_stew` | 0.81 | max |  | Models/kaykit_restaurant/food_stew |
| `food.fries` | 0.12 | max |  | Models/kenney_food/fries |
| `food.frikandel_speciaal` | 0.19 | max |  | Models/kenney_food/frikandel-speciaal |
| `food.hot_dog` | 0.18 | max |  | Models/kenney_food/hot-dog |
| `food.ketchup` | 0.66 | max |  | Models/kaykit_restaurant/ketchup |
| `food.meat_sausage` | 0.12 | max |  | Models/kenney_food/meat-sausage |
| `food.mug` | 0.1 | max |  | Models/kenney_food/mug |
| `food.mustard` | 0.66 | max |  | Models/kaykit_restaurant/mustard |
| `food.pan` | 0.23 | max |  | Models/kenney_food/pan |
| `food.pizza` | 0.25 | max |  | Models/kenney_food/pizza |
| `food.pizza_box` | 0.29 | max |  | Models/kenney_food/pizza-box |
| `food.plate` | 0.27 | max |  | Models/kenney_food/plate |
| `food.plate_dinner` | 0.27 | max |  | Models/kenney_food/plate-dinner |
| `food.sandwich` | 0.13 | max |  | Models/kenney_food/sandwich |
| `food.sausage` | 0.11 | max |  | Models/kenney_food/sausage |
| `food.skewer` | 0.18 | max |  | Models/kenney_food/skewer |
| `food.soda` | 0.13 | max |  | Models/kenney_food/soda |
| `food.soda_can` | 0.11 | max |  | Models/kenney_food/soda-can |
| `food.styrofoam_dinner` | 0.38 | max |  | Models/kenney_food/styrofoam-dinner |
| `food.sub` | 0.2 | max |  | Models/kenney_food/sub |
| `food.taco` | 0.13 | max |  | Models/kenney_food/taco |
| `interior.armchair` | 1.26 | max |  | Models/kaykit_furniture/armchair |
| `interior.armchair_pillows` | 1.26 | max |  | Models/kaykit_furniture/armchair_pillows |
| `interior.bed_single_a` | 2.1 | max |  | Models/kaykit_furniture/bed_single_A |
| `interior.cabinet_medium_decorated` | 1.43 | max |  | Models/kaykit_furniture/cabinet_medium_decorated |
| `interior.chair_a_wood` | 0.88 | max |  | Models/kaykit_furniture/chair_A_wood |
| `interior.chair_desk_a` | 0.9 | max |  | Models/kaykit_furniture/chair_desk_A |
| `interior.chair_desk_b` | 0.83 | max |  | Models/kaykit_furniture/chair_desk_B |
| `interior.couch` | 2.1 | max |  | Models/kaykit_furniture/couch |
| `interior.couch_pillows` | 2.1 | max |  | Models/kaykit_furniture/couch_pillows |
| `interior.desk` | 2.1 | max |  | Models/kaykit_furniture/desk |
| `interior.desk_decorated` | 2.1 | max |  | Models/kaykit_furniture/desk_decorated |
| `interior.desk_large_decorated` | 2.8 | max |  | Models/kaykit_furniture/desk_large_decorated |
| `interior.keyboard` | 0.54 | max |  | Models/kaykit_furniture/keyboard |
| `interior.monitor` | 1.05 | max |  | Models/kaykit_furniture/monitor |
| `interior.mouse` | 0.24 | max |  | Models/kaykit_furniture/mouse |
| `interior.shelf_a_big` | 1.4 | max |  | Models/kaykit_furniture/shelf_A_big |
| `interior.shelf_b_large_decorated` | 1.4 | max |  | Models/kaykit_furniture/shelf_B_large_decorated |
| `interior.shelf_b_small_decorated` | 0.71 | max |  | Models/kaykit_furniture/shelf_B_small_decorated |
| `interior.table_low_decorated` | 1.68 | max |  | Models/kaykit_furniture/table_low_decorated |
| `interior.table_medium` | 1.4 | max |  | Models/kaykit_furniture/table_medium |
| `logistics.barrel_a` | 0.6 | max |  | Models/kaykit_prototype/Barrel_A |
| `logistics.bottle_return` | 1.86 | max |  | Models/kenney_mini_market/bottle-return |
| `logistics.box` | 0.45 | max | Karton | Models/kaykit_prototype/Box_A |
| `logistics.box_a` | 0.31 | max |  | Models/kaykit_prototype/Box_A |
| `logistics.box_b` | 0.36 | max |  | Models/kaykit_prototype/Box_B |
| `logistics.box_c` | 0.48 | max |  | Models/kaykit_prototype/Box_C |
| `logistics.box_large` | 1.1 | max |  | Models/kenney_factory/box-large |
| `logistics.box_long` | 1.1 | max |  | Models/kenney_factory/box-long |
| `logistics.box_small` | 0.59 | max |  | Models/kenney_factory/box-small |
| `logistics.box_wide` | 1.0 | max |  | Models/kenney_factory/box-wide |
| `logistics.can_a` | 0.29 | max |  | Models/kaykit_prototype/Can_A |
| `logistics.cash_register` | 1.44 | max |  | Models/kenney_mini_market/cash-register |
| `logistics.cone` | 0.31 | max |  | Models/kenney_factory/cone |
| `logistics.conveyor` | 1.0 | max |  | Models/kenney_factory/conveyor |
| `logistics.conveyor_long` | 2.0 | max |  | Models/kenney_factory/conveyor-long |
| `logistics.conveyor_long_sides` | 2.0 | max |  | Models/kenney_factory/conveyor-long-sides |
| `logistics.conveyor_long_stripe_sides` | 2.0 | max |  | Models/kenney_factory/conveyor-long-stripe-sides |
| `logistics.conveyor_sides` | 1.0 | max |  | Models/kenney_factory/conveyor-sides |
| `logistics.conveyor_stripe` | 1.0 | max |  | Models/kenney_factory/conveyor-stripe |
| `logistics.display_bread` | 1.19 | max |  | Models/kenney_mini_market/display-bread |
| `logistics.display_fruit` | 1.02 | max |  | Models/kenney_mini_market/display-fruit |
| `logistics.door_wide_open` | 1.8 | max |  | Models/kenney_factory/door-wide-open |
| `logistics.freezer` | 1.36 | max |  | Models/kenney_mini_market/freezer |
| `logistics.freezers_standing` | 1.7 | max |  | Models/kenney_mini_market/freezers-standing |
| `logistics.locker` | 1.8 | max |  | Models/kaykit_prototype/Locker |
| `logistics.locker_decorated` | 1.8 | max |  | Models/kaykit_prototype/Locker_Decorated |
| `logistics.pallet` | 1.2 | max | Europalette | Models/kaykit_prototype/Pallet_Small |
| `logistics.pallet_large` | 2.4 | max |  | Models/kaykit_prototype/Pallet_Large |
| `logistics.pallet_small` | 1.2 | max |  | Models/kaykit_prototype/Pallet_Small |
| `logistics.pallet_small_decorated_a` | 1.2 | max |  | Models/kaykit_prototype/Pallet_Small_Decorated_A |
| `logistics.pallet_small_decorated_b` | 1.8 | max |  | Models/kaykit_prototype/Pallet_Small_Decorated_B |
| `logistics.robot_arm_a` | 3.2 | max |  | Models/kenney_factory/robot-arm-a |
| `logistics.scanner_high` | 1.77 | max |  | Models/kenney_factory/scanner-high |
| `logistics.scanner_low` | 1.6 | max |  | Models/kenney_factory/scanner-low |
| `logistics.screen_flat` | 1.2 | max |  | Models/kenney_factory/screen-flat |
| `logistics.screen_hanging_wide` | 0.9 | max |  | Models/kenney_factory/screen-hanging-wide |
| `logistics.shelf_bags` | 1.47 | max |  | Models/kenney_mini_market/shelf-bags |
| `logistics.shelf_boxes` | 1.44 | max |  | Models/kenney_mini_market/shelf-boxes |
| `logistics.shelf_end` | 1.78 | max |  | Models/kenney_mini_market/shelf-end |
| `logistics.shopping_basket` | 0.59 | max |  | Models/kenney_mini_market/shopping-basket |
| `logistics.shopping_cart` | 0.81 | max |  | Models/kenney_mini_market/shopping-cart |
| `logistics.table_medium` | 1.2 | max |  | Models/kaykit_prototype/table_medium |
| `logistics.table_medium_decorated` | 1.2 | max |  | Models/kaykit_prototype/table_medium_Decorated |
| `logistics.warning_orange` | 1.29 | max |  | Models/kenney_factory/warning-orange |
| `logistics.warning_traffic` | 1.55 | max |  | Models/kenney_factory/warning-traffic |
| `logistics.workbench` | 1.8 | max |  | Models/kaykit_prototype/Workbench |
| `logistics.workbench_decorated` | 1.8 | max |  | Models/kaykit_prototype/Workbench_Decorated |
| `nature.bush` | 1.79 | max |  | Models/kaykit_city/bush |
| `nature.bush_a` | 2.44 | max |  | Models/kaykit_city/bush_A |
| `nature.bush_b` | 3.19 | max |  | Models/kaykit_city/bush_B |
| `nature.bush_c` | 1.84 | max |  | Models/kaykit_city/bush_C |
| `nature.fence` | 3.32 | max |  | Models/kenney_city_suburban/fence |
| `nature.fence_low` | 8.92 | max |  | Models/kenney_city_suburban/fence-low |
| `nature.planter` | 2.8 | max |  | Models/kenney_city_suburban/planter |
| `nature.tree_a` | 5.57 | max |  | Models/kaykit_city/tree_A |
| `nature.tree_b` | 5.12 | max |  | Models/kaykit_city/tree_B |
| `nature.tree_c` | 5.23 | max |  | Models/kaykit_city/tree_C |
| `nature.tree_d` | 5.04 | max |  | Models/kaykit_city/tree_D |
| `nature.tree_e` | 5.02 | max |  | Models/kaykit_city/tree_E |
| `nature.tree_large` | 5.37 | max |  | Models/kenney_city_suburban/tree-large |
| `nature.tree_small` | 3.97 | max |  | Models/kenney_city_suburban/tree-small |
| `street.bench` | 1.88 | max |  | Models/kaykit_city/bench |
| `street.box_a` | 0.95 | max |  | Models/kaykit_city/box_A |
| `street.box_b` | 0.8 | max |  | Models/kaykit_city/box_B |
| `street.cone` | 1.02 | max |  | Models/kenney_car_kit/cone |
| `street.construction_barrier` | 1.69 | max |  | Models/kenney_city_roads/construction-barrier |
| `street.construction_cone` | 0.7 | max |  | Models/kenney_city_roads/construction-cone |
| `street.construction_light` | 1.75 | max |  | Models/kenney_city_roads/construction-light |
| `street.detail_parasol_a` | 3.38 | max |  | Models/kenney_city_commercial/detail-parasol-a |
| `street.detail_parasol_b` | 3.38 | max |  | Models/kenney_city_commercial/detail-parasol-b |
| `street.dumpster` | 2.66 | max |  | Models/kaykit_city/dumpster |
| `street.firehydrant` | 1.06 | max |  | Models/kaykit_city/firehydrant |
| `street.light_curved` | 5.06 | max |  | Models/kenney_city_roads/light-curved |
| `street.light_square` | 4.5 | max |  | Models/kenney_city_roads/light-square |
| `street.sign_highway` | 7.5 | max |  | Models/kenney_city_roads/sign-highway |
| `street.streetlight` | 4.51 | max |  | Models/kaykit_city/streetlight |
| `street.streetlight_old_double` | 3.29 | max |  | Models/kaykit_city/streetlight_old_double |
| `street.streetlight_old_single` | 3.29 | max |  | Models/kaykit_city/streetlight_old_single |
| `street.trafficlight_a` | 3.43 | max |  | Models/kaykit_city/trafficlight_A |
| `street.trafficlight_b` | 4.51 | max |  | Models/kaykit_city/trafficlight_B |
| `street.trafficlight_c` | 4.56 | max |  | Models/kaykit_city/trafficlight_C |
| `street.trash_a` | 0.63 | max |  | Models/kaykit_city/trash_A |
| `street.trash_b` | 0.33 | max |  | Models/kaykit_city/trash_B |
| `vehicle.ambulance` | 5.59 | max |  | Models/kenney_car_kit/ambulance |
| `vehicle.car_hatchback` | 3.79 | max |  | Models/kaykit_city/car_hatchback |
| `vehicle.car_police` | 4.41 | max |  | Models/kaykit_city/car_police |
| `vehicle.car_sedan` | 4.41 | max |  | Models/kaykit_city/car_sedan |
| `vehicle.car_stationwagon` | 4.41 | max |  | Models/kaykit_city/car_stationwagon |
| `vehicle.car_taxi` | 4.41 | max |  | Models/kaykit_city/car_taxi |
| `vehicle.delivery` | 5.59 | max |  | Models/kenney_car_kit/delivery |
| `vehicle.delivery_flat` | 5.59 | max |  | Models/kenney_car_kit/delivery-flat |
| `vehicle.firetruck` | 5.85 | max |  | Models/kenney_car_kit/firetruck |
| `vehicle.garbage_truck` | 5.93 | max |  | Models/kenney_car_kit/garbage-truck |
| `vehicle.hatchback_sports` | 4.9 | max |  | Models/kenney_car_kit/hatchback-sports |
| `vehicle.kombi` | 4.6 | max | Kombi (Spieler-Auto) | Models/kaykit_city/car_stationwagon |
| `vehicle.police` | 5.33 | max |  | Models/kenney_car_kit/police |
| `vehicle.race` | 4.4 | max |  | Models/kenney_car_kit/race |
| `vehicle.sedan` | 4.39 | max |  | Models/kenney_car_kit/sedan |
| `vehicle.sedan_sports` | 4.39 | max |  | Models/kenney_car_kit/sedan-sports |
| `vehicle.sportscar` | 4.4 | max | Sportwagen | Models/kenney_car_kit/sedan-sports |
| `vehicle.suv` | 4.64 | max |  | Models/kenney_car_kit/suv |
| `vehicle.suv_luxury` | 4.9 | max |  | Models/kenney_car_kit/suv-luxury |
| `vehicle.taxi` | 4.73 | max |  | Models/kenney_car_kit/taxi |
| `vehicle.traffic_1` | 4.5 | max | Verkehrsauto | Models/kaykit_city/car_sedan |
| `vehicle.traffic_2` | 3.9 | max | Verkehrsauto | Models/kaykit_city/car_hatchback |
| `vehicle.traffic_3` | 4.5 | max | Taxi | Models/kaykit_city/car_taxi |
| `vehicle.traffic_4` | 4.6 | max | SUV | Models/kenney_car_kit/suv |
| `vehicle.traffic_5` | 4.4 | max | Limousine | Models/kenney_car_kit/sedan |
| `vehicle.truck` | 5.07 | max |  | Models/kenney_car_kit/truck |
| `vehicle.truck_flat` | 4.72 | max |  | Models/kenney_car_kit/truck-flat |
| `vehicle.van` | 4.73 | max |  | Models/kenney_car_kit/van |
