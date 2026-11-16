# ART_DIRECTION — direction artistique de TennisPhone

Charte visuelle et sonore de la v1. Elle remplace le visuel provisoire des étapes c et d par une identité propre, faisable uniquement avec des primitives Unity (cube, sphère, capsule, cylindre, quad), des matériaux Unity et du son de synthèse.

Date : 2026-11-16. Rien de ce document n'est encore implémenté : la liste des tâches est en section 10.

## 0. Point de départ et contraintes techniques

Ce qui existe aujourd'hui, et que cette charte fait évoluer sans changer le gameplay :

| Élément | Fichier | État actuel |
|---|---|---|
| Court, lignes, filet | `Assets/TennisPhone/Scripts/Court/CourtBuilder.cs` | Vert sur vert, 12 bandes alternées, filet en 16 panneaux |
| Personnages | `RallyController.BuildCharacter` | Une capsule et une sphère, bleu pour le joueur, orange pour l'adversaire |
| Raquettes | `Assets/TennisPhone/Scripts/Player/RacketModel.cs` | Manche, cœur, cadre rouge, cordage crème ; le cordage est renvoyé mais pas utilisé |
| Balle, ombre, marque de rebond | `RallyController.BuildActors` | Sphère grossie 2,4 fois (diamètre affiché 0,158 m), disque sombre au sol, disque vert ou rouge au rebond |
| Affichage | `Assets/TennisPhone/Scripts/DebugUI/RallyHud.cs` | IMGUI, référence de 720 px de haut, police par défaut |
| Caméra et lumière | `Assets/TennisPhone/Editor/SceneBuilder.cs` | Caméra en (0 ; 5,2 ; −23) visant (0 ; 0 ; −4), champ 40°, fond bleu uni, soleil Euler (52 ; −35 ; 0) |
| Son | — | Aucun |

Faits vérifiés dans le projet, dont dépendent les choix ci-dessous :

- Pipeline de rendu intégré (pas d'URP), shader `Standard`, espace colorimétrique **Gamma** : un code hexadécimal donne la couleur attendue sans conversion.
- Ni TextMeshPro ni uGUI dans `Packages/manifest.json` : le HUD reste en **IMGUI** avec une police TTF dynamique.
- `CourtBuilder.Primitive` crée un matériau par objet (`renderer.material`). Avec le décor, cela ferait plusieurs centaines de matériaux : la tâche 1 introduit un cache de matériaux partagés.
- Repère : x en largeur, y en hauteur, z en profondeur. Le joueur est du côté z négatif, l'adversaire du côté z positif. Le court fait 8,23 × 23,77 m, les abords 22,23 × 45,77 m.

## 1. Identité : « Soir des lampions »

**Intention.** Un court de club de quartier, un soir de fin d'été : la lumière est dorée, des guirlandes de lampions sont déjà allumées, on joue entre amis avant le dîner. Tout est fait de formes pleines et mates, comme des jouets en bois peint posés sur une table. La scène est chaleureuse mais le jeu reste lisible avant tout : le court est sombre et froid, la balle est le seul objet jaune-vert de l'image.

**Ce qui distingue le jeu des jeux de sport existants.**

| Choix habituel du genre | Choix de TennisPhone |
|---|---|
| Stade, tribunes pleines, plein midi, ciel bleu | Petit club entouré de haies et de peupliers, soleil bas, lampions |
| Gazon vert sur fond vert, ou dur bleu | Court « sarcelle » (bleu-vert profond) sur abords « brique » |
| Public de personnages | Aucun spectateur : gradins en bois avec des coussins de couleur, parasols, chaise d'arbitre vide |
| Avatars à grosse tête et mains détachées | Figurines articulées en bois peint : tête ovale sans visage, bras et jambes reliés au corps |
| Bandeau de score façon télévision, portraits | « Ardoises » : panneaux sombres aux coins arrondis, texte crème, accents moutarde |
| Sons réalistes ou voix | Percussions de bois synthétisées (« poc », « toc », motifs de marimba) |

**Références fournies par l'utilisateur.** Des captures d'un jeu de tennis familial sur console ont été données comme référence. On en retient les principes généraux, qui ne sont propres à aucun jeu :

- des bandes alternées sur la surface, qui donnent la profondeur ;
- un fond sombre et uni derrière l'adversaire, pour détacher la balle ;
- une ombre ronde sous la balle, qui indique où elle se trouve au-dessus du sol ;
- une balle plus grosse que nature ;
- très peu d'interface pendant l'échange ;
- des couleurs franches et des matériaux mats.

On ne reprend pas : les personnages à tête ronde et mains flottantes, le stade et ses tribunes de spectateurs, les panneaux et logos, le bandeau de score à portraits et à pastilles, la balle bicolore, le gazon vert sur murs verts comme composition d'ensemble. Une variante de surface en gazon est proposée en 2.6 pour qui préfère cette teinte : elle garde tout le reste de l'identité.

**Règles de lisibilité, valables partout.**

1. La balle est le seul objet de teinte jaune-vert (teinte 55° à 95°) de la scène 3D. Aucun lampion, coussin ou vêtement dans cette plage. Le moutarde est réservé à l'interface.
2. Derrière la zone de jeu, le fond est sombre jusqu'en haut de l'écran (haie puis rideau de peupliers).
3. Aucune ombre portée du décor ne tombe sur le court.
4. Une information ne passe jamais par la couleur seule : elle a aussi une forme, une position ou un mot.
5. Aucun texte en dessous de 18 px sur la référence de 720 px : le joueur est debout à deux ou trois mètres de l'écran.

## 2. Palette

Toutes les couleurs vivent dans une seule classe `Palette` (tâche 1). Les rapports de contraste indiqués sont calculés avec la formule WCAG 2.1 sur les couleurs sRGB, avant éclairage.

### 2.1 Court et terrain

| Nom | Hex | Usage |
|---|---|---|
| Sarcelle | `#1F8391` | Bandes paires du court |
| Sarcelle profonde | `#1B7885` | Bandes impaires du court |
| Brique | `#9E4A31` | Abords |
| Crème | `#FFF4DC` | Lignes, bande et sangle du filet |
| Encre | `#17262B` | Mailles et montants du filet, câbles, manches de raquette |
| Bois foncé | `#7A4E2D` | Poteaux du filet, mâts, poteaux de guirlande |
| Pré | `#6E8C4A` | Sol au-delà des abords, jusqu'à l'horizon |

Contrastes utiles au jeu : balle sur sarcelle 3,5:1, sur sarcelle profonde 4,1:1, sur brique 4,8:1, sur haie 6,8:1 ; lignes sur sarcelle 4,1:1, sur brique 5,5:1.

### 2.2 Décor et ciel

| Nom | Hex | Usage |
|---|---|---|
| Haie | `#2C5446` | Haies du fond et des côtés |
| Haie claire | `#3B6B55` | Dessus des haies |
| Peuplier sombre | `#24483C` | Feuillage du rideau de fond, un arbre sur deux |
| Peuplier | `#2F5D4A` | Feuillage du rideau de fond, l'autre arbre sur deux |
| Tilleul | `#3D7357` | Feuillage des arbres de côté |
| Tronc | `#5A3F2C` | Troncs |
| Bois clair | `#D9A066` | Gradins (marches 1 et 3), tables, chaise d'arbitre |
| Bois miel | `#C98F58` | Gradin (marche 2), tabourets |
| Corail | `#FF6B4A` | Lampions, coussins, un parasol |
| Rose | `#FF8FB0` | Lampions, coussins |
| Crème chaude | `#FFF1D0` | Lampions, coussins, un parasol |
| Turquoise | `#3FD0C9` | Lampions, coussins, un parasol |
| Colline proche | `#7FA0A8` | Colline de gauche |
| Colline lointaine | `#9DB7B5` | Colline de droite |
| Nuage | `#FFE6D2` | Nuages bas |
| Ciel, horizon | `#FFD9A8` | Bas du dégradé, couleur de fond de la caméra |
| Ciel, milieu | `#F9C39A` | Milieu du dégradé |
| Ciel, haut | `#9CBFD4` | Haut de l'écran |
| Brume | `#F3CBA4` | Brouillard de distance |

### 2.3 Personnages et raquettes

| Nom | Hex | Usage |
|---|---|---|
| Bois naturel | `#E2B88A` | Tête, cou, bras, mains, jambes des deux figurines |
| Crème | `#FFF4DC` | Maillot du joueur, chaussures, bandeau de l'adversaire |
| Framboise | `#E0457B` | Couleur du joueur : bande du maillot, short, casquette, cadre de raquette |
| Lavande | `#C4B0FF` | Couleur de l'adversaire : maillot |
| Prune | `#4A3580` | Adversaire : bande du maillot, short, cadre de raquette |
| Prune nuit | `#2E2350` | Adversaire : calotte de cheveux |
| Cordage | `#F4EFE2` | Tamis des deux raquettes |
| Blanc | `#FFFFFF` | Éclair du cordage à l'impact, bande de la balle |

Contrastes : maillot crème du joueur sur sarcelle 4,1:1 et sur brique 5,5:1 ; maillot lavande de l'adversaire sur sarcelle 2,3:1, sur brique 3,2:1, sur haie 4,5:1 ; bande prune sur lavande 5,2:1.

### 2.4 Balle et effets

| Nom | Hex | Usage |
|---|---|---|
| Jaune balle | `#D9F233` | Balle (couleur actuelle, conservée) |
| Émission balle | `#4A5212` | Émission du matériau de la balle, pour qu'elle reste vive à l'ombre |
| Balle morte | `#9AA08A` | Balle après la fin du point |
| Ombre | `#0B1D22`, alpha 0,45 | Ombre ronde de la balle |
| Traînée à plat | `#FFF4DC` | Traînée d'un coup sans effet |
| Traînée lift | `#FF9475` | Traînée d'un coup lifté |
| Traînée slice | `#BDF2EE` | Traînée d'un coup slicé |
| Menthe claire | `#7BE8A8` | Marque d'un rebond dans le court |

### 2.5 Interface

| Nom | Hex | Usage |
|---|---|---|
| Ardoise | `#1F2A30`, opacité 92 % | Fond de tous les panneaux |
| Cellule | `#31424B` | Cases de l'ardoise de score, étape de service active |
| Piste | `#45585F` | Piste de la barre de timing et de la jauge de lancer |
| Crème | `#FFF4DC` | Texte principal |
| Gris clair | `#B8C6C4` | Texte secondaire, légendes |
| Moutarde | `#FFC53D` | Points du jeu en cours, messages « Jeu » et « Set », pastilles d'étape |
| Menthe | `#4FD08A` | Zone parfaite, « Point pour toi », téléphone connecté |
| Orange | `#FF9F45` | « Dans le vide », avertissements |
| Rouge | `#B8322A` | « Faute », « Double faute », « Dehors », « Filet » |
| Framboise | `#E0457B` | Pastille du joueur |
| Lavande | `#C4B0FF` | Pastille de l'adversaire |

Contraste texte sur fond, vérifié :

| Texte | Fond | Rapport | Seuil visé |
|---|---|---|---|
| Crème | Ardoise opaque | 13,4:1 | 4,5:1 |
| Crème | Ardoise à 92 % sur un fond blanc (pire cas) | 10,5:1 | 4,5:1 |
| Crème | Cellule | 9,6:1 | 4,5:1 |
| Gris clair | Ardoise opaque | 8,3:1 | 4,5:1 |
| Gris clair | Ardoise à 92 % sur un fond blanc (pire cas) | 6,5:1 | 4,5:1 |
| Ardoise | Moutarde | 9,3:1 | 4,5:1 |
| Ardoise | Menthe | 7,5:1 | 4,5:1 |
| Ardoise | Orange | 7,2:1 | 4,5:1 |
| Crème | Rouge | 5,5:1 | 4,5:1 |
| Ardoise | Crème (carte du code QR) | 13,4:1 | 4,5:1 |

Éléments graphiques sans texte (seuil 3:1) : pastille framboise sur ardoise 3,7:1 ; pastille lavande sur ardoise 7,7:1 ; point de service jaune balle sur ardoise 11,7:1 ; zone menthe sur piste 3,8:1 ; curseur crème sur piste 6,8:1 ; repère moutarde sur piste 4,7:1. La piste elle-même ne ressort qu'à 2,0:1 sur l'ardoise : ses deux extrémités sont donc marquées par un trait crème.

Le texte n'est jamais posé directement sur la scène 3D : il est toujours dans un panneau ardoise ou une pastille de couleur.

### 2.6 Variante de surface : gazon tondu

Pour un court vert, seules deux couleurs changent ; le reste de la charte est identique.

| Nom | Hex | Remplace |
|---|---|---|
| Gazon | `#3F8F4A` | Sarcelle |
| Gazon tondu | `#378242` | Sarcelle profonde |

Contrastes : balle sur gazon 3,2:1 et 3,8:1 ; lignes sur gazon 3,7:1 et 4,3:1 ; maillot lavande 2,1:1. La sarcelle reste le choix par défaut : sur du vert, la balle ne se détache plus que par sa clarté, plus par sa teinte. Le choix se fait par un champ `Palette.Surface` (énumération `Sarcelle`, `Gazon`). La physique du rebond ne change pas : elle reste réglée dans `GameplayConfig`.

## 3. Matériaux

Trois recettes suffisent. Chaque couleur a un seul matériau partagé, créé à la demande par `Palette`.

| Recette | Shader | Réglages | Usage |
|---|---|---|---|
| Mat | `Standard` | `_Glossiness` 0,08 ; `_Metallic` 0 | Tout ce qui est éclairé : court, décor, figurines, raquettes |
| Lumineux | `Standard` | Mat, plus mot-clé `_EMISSION` et `_EmissionColor` | Lampions (émission = couleur × 0,9), balle (émission `#4A5212`) |
| Voile | `Sprites/Default` | Non éclairé ; teinte et transparence par la couleur du matériau ; accepte une texture | Ombre de la balle, traînée, anneau d'impact, ciel, nuages |

Tous les objets en recette Voile ont `shadowCastingMode = Off` et `receiveShadows = false`.

Deux points à vérifier à l'implémentation :

- Le ciel et les nuages sont au-delà de la fin de la brume (160 m). Ils doivent donc utiliser un shader qui n'applique pas la brume. Les shaders `Unlit/Color` et `Unlit/Texture` l'appliquent : le ciel y deviendrait un aplat de la couleur de brume. `Sprites/Default` est choisi pour cette raison ; si le dégradé n'apparaît pas à l'écran, c'est ce point qu'il faut regarder en premier.
- Avant un build autonome : un shader obtenu par `Shader.Find` et une variante activée par mot-clé (`_EMISSION`) peuvent être retirés du build si aucun asset ne les référence. La parade est déjà utilisée dans le projet pour `Sol.mat` : créer un matériau modèle par recette dans `SceneBuilder` (`Assets/TennisPhone/Config/Lumineux.mat`, `Voile.mat`) et les faire référencer par la scène. Dans l'éditeur, le problème ne se pose pas.

## 4. Personnages

### 4.1 Principe

Deux figurines en bois peint, de même construction. Tête ovale sans visage, membres reliés au corps, mains fixées au bout des bras. L'orientation se lit par la coiffe et par les bras, pas par un visage. Les parties « peau » sont en bois naturel pour les deux : la figurine ne représente personne en particulier.

Origine de la figurine : entre les pieds, au sol. La figurine regarde vers +z ; celle de l'adversaire est tournée de 180° autour de y.

### 4.2 Pièces communes (figurine de 1,74 m)

Rappel des primitives Unity à l'échelle 1 : cube de 1 m de côté ; sphère de 1 m de diamètre ; cylindre de 1 m de diamètre et 2 m de haut ; capsule de 1 m de diamètre et 2 m de haut.

| Pièce | Primitive | Position locale (x ; y ; z) | Échelle (x ; y ; z) | Taille réelle | Couleur |
|---|---|---|---|---|---|
| Chaussures (2) | Cube | (±0,11 ; 0,04 ; 0,04) | (0,11 ; 0,08 ; 0,26) | 26 cm de long | Crème |
| Jambes (2) | Capsule | (±0,11 ; 0,46 ; 0) | (0,15 ; 0,40 ; 0,15) | Ø 15 cm, 80 cm de haut | Bois naturel |
| Short | Cylindre | (0 ; 0,80 ; 0) | (0,40 ; 0,12 ; 0,28) | 40 × 28 cm, 24 cm de haut | Couleur du camp |
| Torse | Capsule | (0 ; 1,14 ; 0) | (0,44 ; 0,31 ; 0,28) | 44 × 28 cm, 62 cm de haut | Maillot |
| Bande du maillot | Cylindre | (0 ; 1,20 ; 0) | (0,455 ; 0,07 ; 0,29) | 14 cm de haut, dépasse le torse de 7 mm | Couleur du camp |
| Cou | Cylindre | (0 ; 1,47 ; 0) | (0,10 ; 0,03 ; 0,10) | Ø 10 cm | Bois naturel |
| Tête | Capsule | (0 ; 1,615 ; 0) | (0,22 ; 0,125 ; 0,23) | 22 cm de large, 25 cm de haut | Bois naturel |
| Bras (2) | Capsule | Du point d'épaule (±0,25 ; 1,36 ; 0) à la main | (0,10 ; longueur ÷ 2 ; 0,10) | Ø 10 cm, 40 à 75 cm | Bois naturel |
| Mains (2) | Sphère | Au bout du bras | (0,11 ; 0,11 ; 0,11) | Ø 11 cm | Bois naturel |
| Bracelet | Cylindre | À 85 % du bras qui tient la raquette | (0,115 ; 0,025 ; 0,115) | 5 cm de haut | Couleur du camp |

Bras : chaque bras est une capsule placée au milieu du segment épaule–main et orientée par `Quaternion.FromToRotation(Vector3.up, main − épaule)`. Sa longueur suit la distance, bornée entre 0,40 et 0,75 m.

- Bras de la raquette : il part de l'épaule de la main dominante (`GameplayConfig.handedness`) et rejoint le point où `RallyController.UpdateVisuals` place déjà la raquette, soit (côté de frappe × 0,35 ; 0,95 ; 0,15) par rapport à la figurine. En revers, le bras croise devant le torse et s'allonge : c'est voulu.
- Bras libre : main au repos en (∓0,31 ; 0,93 ; 0,05).

### 4.3 Ce qui différencie le joueur de l'adversaire

| | Joueur (« Toi ») | Adversaire |
|---|---|---|
| Vu | De dos, de près : 144 px de haut à 720p | De face, de loin : 51 px de haut |
| Taille | 1,74 m | 1,84 m : racine mise à l'échelle (0,97 ; 1,06 ; 0,97) |
| Maillot | Crème `#FFF4DC` | Lavande `#C4B0FF` |
| Bande, short, bracelet | Framboise `#E0457B` | Prune `#4A3580` |
| Coiffe | Casquette framboise portée à l'envers : la visière pointe vers la caméra | Calotte de cheveux prune nuit et bandeau crème |
| Raquette | Cadre et cœur framboise | Cadre et cœur prune |
| Pastille dans le HUD | Framboise | Lavande |

Pièces de coiffe :

| Pièce | Primitive | Position locale | Échelle | Couleur |
|---|---|---|---|---|
| Casquette, calotte (joueur) | Sphère | (0 ; 1,685 ; 0) | (0,235 ; 0,13 ; 0,245) | Framboise |
| Casquette, visière (joueur) | Cube | (0 ; 1,66 ; −0,15) | (0,17 ; 0,018 ; 0,12) | Framboise |
| Cheveux (adversaire) | Sphère | (0 ; 1,70 ; 0) | (0,215 ; 0,10 ; 0,225) | Prune nuit |
| Bandeau (adversaire) | Cylindre | (0 ; 1,66 ; 0) | (0,228 ; 0,022 ; 0,238) | Crème |

Le joueur est clair avec un accent chaud, l'adversaire est lavande avec un accent sombre : la différence tient à la clarté et à la silhouette de la coiffe, pas seulement à la teinte.

### 4.4 Animation par le code

Aucun squelette. Trois mouvements suffisent, tous appliqués à la racine ou aux bras :

| Mouvement | Valeur |
|---|---|
| Respiration à l'arrêt | Torse et tête montent et descendent de ±1,5 cm à 1,6 Hz |
| Course | Racine inclinée de 6° vers la direction du déplacement ; rebond vertical de 3 cm à 3 Hz |
| Frappe de l'adversaire | Balayage actuel de la raquette (−70° à +70° en 0,3 s) conservé ; le bras suit la raquette |

### 4.5 Raquettes

La géométrie de `RacketModel.Build` est conservée (manche, cœur, cadre, cordage). Seules les couleurs changent, et `Build` reçoit la couleur du camp.

| Pièce | Couleur |
|---|---|
| Manche | Encre `#17262B` |
| Cœur et cadre | Couleur du camp (framboise ou prune) |
| Cordage | `#F4EFE2` |

Le cordage clair est la partie la plus visible : il se détache à 4:1 sur le court. À l'impact, il passe au blanc pur puis revient à sa couleur en 0,12 s (section 6.4).

## 5. Court et décor

### 5.1 Repères à l'écran

Positions verticales à l'écran avec la caméra actuelle, en pixels depuis le haut sur une référence de 1280 × 720. Elles servent à placer le décor et le HUD.

| Repère | y |
|---|---|
| Horizon | 89 |
| Haut de la haie du fond | 147 |
| Tête de l'adversaire sur sa ligne de fond | 187 |
| Ligne de fond de l'adversaire | 242 |
| Haut du filet au centre | 278 |
| Sommet d'un lancer de service à 3 m | 296 |
| Pied du filet | 316 |
| Tête du joueur | 416 |
| Ligne de fond du joueur | 530 |
| Pieds du joueur | 560 |

Le ciel n'occupe que les 89 px du haut. Le décor est donc une frise en haut de l'image et deux bords, pas un panorama.

### 5.2 Sol, lignes et filet

| Élément | Primitive | Position | Échelle | Couleur |
|---|---|---|---|---|
| Pré | Cube | (0 ; −0,11 ; 40) | (360 ; 0,1 ; 300) | Pré |
| Abords | Cube (existant) | (0 ; −0,05 ; 0) | (22,23 ; 0,1 ; 45,77) | Brique |
| Court | 12 cubes (existants) | Bandes de 1,98 m de profondeur | (8,23 ; 0,002 ; 1,98) | Sarcelle et sarcelle profonde en alternance |
| Lignes | Cubes (existants) | Largeurs actuelles : 5 cm, 10 cm pour les lignes de fond | — | Crème |
| Mailles et montants du filet | Cubes (existants) | — | — | Encre |
| Bande et sangle du filet | Cubes (existants) | — | — | Crème |
| Poteaux du filet | Cubes (existants) | — | — | Bois foncé |
| Boules de poteau | Sphère (2) | Au sommet de chaque poteau | (0,11 ; 0,11 ; 0,11) | Crème |

### 5.3 Fond sombre : haies et peupliers

| Élément | Primitive | Position | Échelle | Couleur |
|---|---|---|---|---|
| Haie du fond | Cube | (0 ; 1,3 ; 24,6) | (30 ; 2,6 ; 1,4) | Haie |
| Dessus de la haie du fond | Cube | (0 ; 2,63 ; 24,6) | (30 ; 0,06 ; 1,4) | Haie claire |
| Haies de côté (2) | Cube | (±12,6 ; 0,9 ; 1) | (1,2 ; 1,8 ; 48) | Haie |
| Dessus des haies de côté (2) | Cube | (±12,6 ; 1,83 ; 1) | (1,2 ; 0,06 ; 48) | Haie claire |

Rideau de peupliers, deux rangs en quinconce, qui ferme le fond jusqu'en haut de l'écran entre x = −16 et x = +16 :

| Rang | z | x | Nombre |
|---|---|---|---|
| 1 | 29 | −16, −12, −8, −4, 0, 4, 8, 12, 16 | 9 |
| 2 | 33 | −14, −10, −6, −2, 2, 6, 10, 14 | 8 |

Un peuplier :

| Pièce | Primitive | Position | Échelle | Couleur |
|---|---|---|---|---|
| Tronc | Cylindre | (x ; 1,2 ; z) | (0,5 ; 1,2 ; 0,5) | Tronc |
| Feuillage | Capsule | (x ; 6,6 ; z) | (3,4 ; 4,6 × k ; 3,4) | Peuplier sombre et peuplier en alternance |

k varie de 0,92 à 1,08 d'un arbre à l'autre, tiré avec une graine fixe (`new System.Random(7)`) pour que le décor soit identique à chaque lancement.

Arbres de côté (tilleuls), visibles au-dessus des haies latérales :

| Position du tronc (x ; z) | Tronc : cylindre | Feuillage : sphère |
|---|---|---|
| (−19 ; −4), (−21 ; 9), (−19 ; 21), (19 ; 3), (21 ; 15) | Position (x ; 1,5 ; z), échelle (0,6 ; 1,5 ; 0,6), couleur tronc | Position (x ; 5,5 ; z), échelle (6 ; 5,5 ; 6), couleur tilleul |

### 5.4 Mobilier

**Gradins en bois, côté gauche.** Trois marches de 14 m le long du court, sans spectateurs.

| Élément | Primitive | Position | Échelle | Couleur |
|---|---|---|---|---|
| Marche 1 | Cube | (−8,6 ; 0,225 ; 0) | (0,9 ; 0,45 ; 14) | Bois clair |
| Marche 2 | Cube | (−9,5 ; 0,45 ; 0) | (0,9 ; 0,90 ; 14) | Bois miel |
| Marche 3 | Cube | (−10,4 ; 0,675 ; 0) | (0,9 ; 1,35 ; 14) | Bois clair |
| Coussins (16) | Cube | Posés sur les marches, z de −6,3 à +6,3 par pas de 0,84 m, marche tirée avec la graine 7 | (0,42 ; 0,07 ; 0,38) | Cycle corail, turquoise, crème chaude, rose, lavande |

**Parasols et tables, côté droit.** Trois ensembles en x = 9,6 et z = −5, 1 et 7.

| Élément | Primitive | Position | Échelle | Couleur |
|---|---|---|---|---|
| Mât | Cylindre | (9,6 ; 1,25 ; z) | (0,07 ; 1,25 ; 0,07) | Bois foncé |
| Toile basse | Cylindre | (9,6 ; 2,42 ; z) | (2,6 ; 0,05 ; 2,6) | Corail, crème chaude, turquoise (un par parasol) |
| Toile haute | Cylindre | (9,6 ; 2,54 ; z) | (1,5 ; 0,07 ; 1,5) | Même couleur |
| Pommeau | Sphère | (9,6 ; 2,66 ; z) | (0,16 ; 0,16 ; 0,16) | Crème |
| Table | Cylindre | (9,6 ; 0,74 ; z) | (0,9 ; 0,02 ; 0,9) | Bois clair |
| Tabourets (2) | Cylindre | (9,6 ; 0,225 ; z ± 0,75) | (0,36 ; 0,225 ; 0,36) | Bois miel |

**Chaise d'arbitre, côté droit, dans l'axe du filet.** Vide.

| Élément | Primitive | Position | Échelle | Couleur |
|---|---|---|---|---|
| Montants (4) | Cube | (6,3 ± 0,3 ; 0,95 ; ±0,3) | (0,06 ; 1,9 ; 0,06) | Bois clair |
| Barreaux (3) | Cube | (6,3 ; 0,5 / 1,0 / 1,5 ; −0,3) | (0,6 ; 0,04 ; 0,04) | Bois clair |
| Assise | Cube | (6,3 ; 1,9 ; 0) | (0,7 ; 0,06 ; 0,7) | Crème |
| Dossier | Cube | (6,62 ; 2,25 ; 0) | (0,06 ; 0,7 ; 0,7) | Crème |

Tout le décor est sans collision (`CourtBuilder.Primitive` retire déjà les colliders) : il ne change rien à la physique de la balle.

### 5.5 Lampions

Le motif qui signe l'ambiance. Aucune source de lumière réelle : les lampions sont des sphères émissives.

| Élément | Valeur |
|---|---|
| Poteaux | 6 cylindres en (±11,6 ; 2,8 ; z) pour z = −16, 2 et 20 ; échelle (0,14 ; 2,8 ; 0,14) ; bois foncé |
| Guirlandes de côté | 2 travées par côté (z de −16 à 2, puis de 2 à 20), accrochées à 5,5 m |
| Guirlande du fond | 1 travée de (−11,6 ; 5,5 ; 20) à (11,6 ; 5,5 ; 20) |
| Flèche du câble | 0,7 m au milieu des travées de côté, 0,9 m pour celle du fond : y = 5,5 − flèche × 4u(1 − u), u de 0 à 1 le long de la travée |
| Lampions par travée | 8 sur les côtés, 10 au fond, en u = (i + 0,5) ÷ nombre ; 42 au total |
| Un lampion | Sphère de Ø 0,34 m, centre 0,22 m sous le câble ; recette Lumineux |
| Capuchon | Cylindre (0,10 ; 0,03 ; 0,10) au sommet du lampion, encre |
| Couleurs | Cycle corail, crème chaude, turquoise, rose. Jamais de jaune ni de vert |
| Câble | 8 cubes de 2 cm d'épaisseur par travée, chacun tendu entre deux points consécutifs de la courbe, encre |

Animation : chaque lampion oscille de ±3° à 0,4 Hz avec une phase décalée. À la fin d'un set, l'émission de tous les lampions pulse de 0,9 à 1,6 trois fois en 1,5 s.

### 5.6 Lointain et ciel

| Élément | Primitive | Position | Échelle | Couleur, recette |
|---|---|---|---|---|
| Colline de gauche | Sphère | (−55 ; −2 ; 110) | (120 ; 22 ; 40) | Colline proche, Mat |
| Colline de droite | Sphère | (45 ; −3 ; 120) | (150 ; 30 ; 40) | Colline lointaine, Mat |
| Nuages (3 groupes de 3) | Sphère | Groupes centrés en (−70 ; 12 ; 125), (20 ; 13 ; 125), (80 ; 11 ; 125) ; sphères décalées de ±6 m en x | De (14 ; 2,2 ; 4) à (20 ; 3 ; 4) | Nuage, Voile |
| Ciel | Quad enfant de la caméra | Position locale (0 ; 0 ; 170) | (300 ; 125 ; 1) | Texture de dégradé, Voile |

Dégradé du ciel : une texture de 4 × 256 px générée par le code, en `TextureWrapMode.Clamp`, lue de bas en haut.

| Hauteur dans la texture (0 = bas de l'écran) | Couleur |
|---|---|
| De 0 à 0,875 | `#FFD9A8` (caché par le sol, sert de repli) |
| 0,93 | `#F9C39A` |
| 1 | `#9CBFD4` |

Le quad couvre le champ de la caméra jusqu'au format 21:9. Il est valable parce que la caméra ne tourne pas ; si son cadrage change, la hauteur 0,875 (l'horizon) est à recalculer. Aucun soleil visible : la lumière vient de derrière la caméra.

### 5.7 Caméra, lumière et atmosphère

Le cadrage ne change pas. Seuls le fond et la profondeur de vue sont modifiés.

| Réglage | Valeur actuelle | Nouvelle valeur |
|---|---|---|
| `camera.backgroundColor` | (0,55 ; 0,75 ; 0,92) | `#FFD9A8` |
| `camera.farClipPlane` | 120 | 180 |
| Position, visée, champ | (0 ; 5,2 ; −23), vers (0 ; 0 ; −4), 40° | Inchangés |

Soleil (lumière directionnelle unique) :

| Réglage | Valeur |
|---|---|
| Rotation | Euler (38 ; 28 ; 0) : le soleil est derrière la caméra, à gauche, à 38° au-dessus de l'horizon |
| Couleur | `#FFE3BD` |
| Intensité | 1,25 |
| Ombres | `LightShadows.Soft`, `shadowStrength` 0,6, `shadowBias` 0,05, `shadowNormalBias` 0,4 |
| Portée des ombres | `QualitySettings.shadowDistance = 70`, `QualitySettings.shadowCascades = 2` |

Avec cette direction, tout ce que la caméra voit de face est éclairé (dos du joueur, face de l'adversaire, haie du fond). Une figurine de 1,74 m porte une ombre de 2,2 m vers l'avant droit. Les ombres des gradins, des haies et des poteaux restent sur les abords : la plus longue (poteau de 5,6 m) s'arrête à x = −8,2, à 4 m de la ligne de côté.

Lumière d'ambiance et brume :

| Réglage | Valeur |
|---|---|
| `RenderSettings.ambientMode` | `AmbientMode.Trilight` |
| `ambientSkyColor` | `#A9C3DE` |
| `ambientEquatorColor` | `#E8C7A6` |
| `ambientGroundColor` | `#6B5548` |
| `RenderSettings.reflectionIntensity` | 0 |
| `RenderSettings.fog` | Activé, `FogMode.Linear` |
| `fogColor` | `#F3CBA4` |
| `fogStartDistance`, `fogEndDistance` | 45 et 160 |

La brume commence derrière la haie du fond (à 46 m de la caméra) : elle n'affecte ni le court ni les joueurs, et fond le pré et les collines dans l'horizon.

## 6. Balle et retours de frappe

### 6.1 Balle

| Élément | Valeur |
|---|---|
| Forme | Sphère, échelle visuelle actuelle conservée : 2,4 fois le rayon réel, soit Ø 0,158 m |
| Couleur | `#D9F233`, recette Lumineux avec émission `#4A5212` |
| Bande | Cylindre enfant de la balle, échelle locale (1,01 ; 0,03 ; 1,01), incliné de 25°, blanc `#FFFFFF` |
| Rotation | La balle tourne autour de l'axe de son effet (`ball.Spin`), à un cinquième de la vitesse réelle pour rester lisible : la bande montre le lift et le slice |
| Ombres réelles | `shadowCastingMode = Off` sur la balle et sa bande : seule l'ombre ronde ci-dessous indique sa position |
| Balle morte | La couleur glisse vers `#9AA08A` en 0,2 s et l'émission s'éteint, pendant le délai `DeadBallLinger` |

### 6.2 Ombre de la balle

Disque à la verticale de la balle, posé à y = 0,005 (comme aujourd'hui), en recette Voile.

| Hauteur h de la balle | Diamètre de l'ombre | Opacité |
|---|---|---|
| 0 m | 1,1 × le diamètre de la balle (0,17 m) | 0,55 |
| 4 m et plus | 1,6 × le diamètre de la balle (0,25 m) | 0,25 |
| Entre les deux | Interpolation linéaire sur h ÷ 4 | Interpolation linéaire |

Couleur `#0B1D22`. L'ombre reste toujours à la verticale, quelle que soit la direction du soleil.

### 6.3 Traînée

Un `TrailRenderer` sur la balle, en recette Voile.

| Réglage | Valeur |
|---|---|
| `time` | 0,18 s |
| `startWidth`, `endWidth` | 0,14 m et 0 |
| `minVertexDistance` | 0,05 m |
| Couleur | Selon l'effet du dernier coup : à plat `#FFF4DC`, lift `#FF9475`, slice `#BDF2EE` |
| Opacité | De 0,7 à la balle à 0 en bout de traînée |
| Affichage | `emitting = true` seulement au-dessus de 12 m/s |
| Remise à zéro | `Clear()` à chaque engagement, service ou repositionnement de la balle |

### 6.4 Retours visuels

Chaque retour combine une forme, une couleur, un mot et un son (section 8).

| Événement | Sur le court | Dans le HUD |
|---|---|---|
| Frappe réussie du joueur | Cordage blanc pendant 0,12 s. Anneau d'impact : cylindre plat face à la caméra, de Ø 0,15 à 0,9 m en 0,18 s, opacité de 0,8 à 0, couleur de la traînée. Recul de la caméra de 4 cm × puissance pendant 0,08 s | Repère moutarde posé sur la barre de timing ; verdict « TÔT », « PARFAIT » ou « TARD » |
| Frappe parfaite (écart sous 25 ms, seuil actuel) | En plus : 6 petits cubes crème de 4 cm projetés en étoile sur 0,5 m en 0,25 s | Verdict « PARFAIT » sur pastille menthe |
| Frappe de l'adversaire | Cordage blanc 0,12 s, anneau d'impact à 60 % de la taille | Rien |
| Swing dans le vide | Aucun éclair. La raquette laisse une traînée grise `#B8C6C4` de 0,15 s | Verdict « DANS LE VIDE » sur pastille orange ; la barre de timing tremble de ±6 px pendant 0,2 s |
| Rebond dans le court | Disque menthe claire `#7BE8A8` de Ø 0,35 m, qui rétrécit à 0 en 0,6 s | Rien |
| Faute (balle dehors) | Croix crème : deux cubes croisés de (0,5 ; 0,004 ; 0,09), fixes pendant 1,2 s. La ligne la plus proche clignote deux fois en blanc | Message « DEHORS » sur fond rouge |
| Balle dans le filet | Le panneau de filet touché recule de 6 cm puis revient en 0,25 s | Message « FILET » sur fond rouge |
| Faute de service | Croix crème dans le carré de service | Message « FAUTE » puis étiquette « 2e service » |

Le rebond bon est un rond, la faute est une croix : la distinction ne repose pas sur le vert et le rouge.

## 7. Interface (HUD)

### 7.1 Principes

- Référence de 1280 × 720, mise à l'échelle par `Screen.height / 720` comme dans `RallyHud`. Les éléments de gauche sont ancrés à gauche, ceux de droite à droite, le reste est centré : le HUD tient du 16:10 au 21:9.
- Panneaux « ardoise » : fond `#1F2A30` à 92 %, coins arrondis de 16 px, sans bordure. Le fond arrondi est une texture blanche de 64 × 64 générée par le code (rectangle arrondi lissé), utilisée en neuf tranches (`GUIStyle.border` de 16) et teintée par `GUI.color`.
- Pendant l'échange, seuls l'ardoise de score et la barre de timing sont visibles. Les grands messages n'apparaissent que balle morte.
- Tutoiement, comme dans les textes actuels (« Toi », « scanne »).

### 7.2 Police

**Baloo 2**, dessinée par Ek Type. Licence **SIL Open Font License 1.1** (« Copyright 2019 The Baloo 2 Project Authors », dépôt `github.com/EkType/Baloo2`), qui autorise l'usage et l'inclusion dans un logiciel, y compris commercial, à condition de joindre la notice et le texte de la licence. Source : `fonts.google.com/specimen/Baloo+2`.

| Fichier à importer | Graisse | Usage |
|---|---|---|
| `Baloo2-Medium.ttf` | 500 | Légendes, aide |
| `Baloo2-SemiBold.ttf` | 600 | Noms, consignes, sous-titres |
| `Baloo2-ExtraBold.ttf` | 800 | Chiffres du score, messages, verdicts |

Dossier : `Assets/TennisPhone/Resources/Fonts/`, avec le fichier `OFL.txt` fourni avec la police. Chargement par `Resources.Load<Font>("Fonts/Baloo2-ExtraBold")`, puis `GUIStyle.font`. Prendre les fichiers statiques (un par graisse), pas le fichier variable. À vérifier à l'import : la présence des glyphes É, È, À, Ç, œ, « · » et « – ». Tant que la police n'est pas importée, la police par défaut d'Unity sert de repli.

### 7.3 Maquette

Vue d'ensemble pendant un échange (1280 × 720) :

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ ┌─ A. Ardoise de score ──────┐                     ┌─ E. Raquette connectée ┐│
│ │ ●▌Toi            1    40   │                     └────────────────────────┘│
│ │  ▌Adversaire     2    30   │                                               │
│ └────────────────────────────┘                                               │
│  (1er service)                                                               │
│                      ┌──── B. Message (balle morte) ────┐                    │
│                      │            ÉGALITÉ               │                    │
│                      └──────────────────────────────────┘                    │
│                                                                              │
│                              (court, joueurs, balle)                         │
│                                                                              │
│                                                                              │
│ ┌ F. Aide ───┐   ┌──── C. Barre de timing / D. Service ────┐                 │
│ │ R · D · T  │   │ [PARFAIT] Coup droit lifté · 82 km/h    │                 │
│ └────────────┘   │ ▏░░░░░░░░░░░░░▓▓▓│▓▓▓░░░░░░░░░░░░░░░░▕  │                 │
│                  │ tôt · croisé    idéal  tard · long de ligne               │
│                  └─────────────────────────────────────────┘                 │
└──────────────────────────────────────────────────────────────────────────────┘
```

Positions et tailles, en pixels de la référence :

| Zone | Ancrage | x | y | Largeur | Hauteur |
|---|---|---|---|---|---|
| A. Ardoise de score | Haut gauche | 24 | 24 | 404 | 128 |
| A′. Étiquette de service | Haut gauche | 24 | 160 | Selon le texte, 40 de marge | 36 |
| B. Message | Centré | — | 172 | Texte + 96, de 380 à 900 | 92 |
| B′. Sous-titre du message | Centré | — | 272 | Texte + 64 | 46 |
| C. Barre de timing | Bas centré | 290 | 600 | 700 | 104 |
| D. Consignes de service | Bas centré | 290 | 600 | 700 | 104 |
| E. État du téléphone | Haut droit | 1280 − 24 − largeur | 24 | Selon le texte | 36 |
| E′. Carte du code QR | Haut droit | 996 | 24 | 260 | 316 |
| F. Aide clavier | Bas gauche | 24 | 648 | 250 | 56 |

### 7.4 A. Ardoise de score

Deux lignes de 52 px (marge intérieure de 12 px en haut et en bas, 14 px sur les côtés), la ligne du joueur au-dessus.

| Colonne | Largeur | Contenu | Style |
|---|---|---|---|
| Serveur | 28 | Disque de Ø 16 jaune balle `#D9F233` sur la ligne du serveur, vide sur l'autre | — |
| Pastille de camp | 8 | Barre de 8 × 32, coins de 4 : framboise pour « Toi », lavande pour « Adversaire » | — |
| Nom | 186 (10 de marge à gauche) | « Toi », « Adversaire » | SemiBold 26, crème |
| Jeux | 52 × 40 | Jeux gagnés dans le set | ExtraBold 30, crème sur case cellule `#31424B`, coins de 8 |
| Points | 84 × 40 (8 de marge à gauche) | « 0 », « 15 », « 30 », « 40 », « Av » | ExtraBold 32, ardoise sur case moutarde `#FFC53D`, coins de 8 |

Les chiffres sont centrés dans des cases de largeur fixe : le panneau ne bouge pas quand le score change. À l'égalité, les deux lignes affichent « 40 ». À l'avantage, le camp qui l'a affiche « Av », l'autre « 40 ». Quand un jeu est gagné, la case des jeux du gagnant passe au moutarde pendant 1 s. Le serveur est donc donné deux fois : par le point jaune et par l'étiquette A′.

Étiquette A′, sous l'ardoise : pastille ardoise de 36 px de haut, coins de 18, texte Medium 20 crème. Contenus : « 1er service », « 2e service », « L'adversaire sert », et hors service « Set en 3 jeux · niveau normal ».

### 7.5 B. Messages

Pastille centrée, coins de 46 px, texte ExtraBold 60 en capitales. Un sous-titre facultatif dans une seconde pastille ardoise, texte SemiBold 26 crème. Apparition : échelle de 0,8 à 1 en 0,12 s (`GUIUtility.ScaleAroundPivot`), puis fondu de 0,3 s à la fin.

| Message | Sous-titre | Fond | Texte | Durée | Son |
|---|---|---|---|---|---|
| POINT POUR TOI | Le score annoncé, par exemple « 30 – 15 » | Menthe | Ardoise | 1,6 s | Point gagné |
| POINT PERDU | Le score annoncé | Ardoise | Crème | 1,6 s | Point perdu |
| DEHORS | « Point pour toi » ou « Point perdu » | Rouge | Crème | 1,6 s | Faute |
| FILET | Idem | Rouge | Crème | 1,6 s | Filet |
| FAUTE | « 2e service » | Rouge | Crème | 1,4 s | Faute |
| DOUBLE FAUTE | « Point pour toi » ou « Point perdu » | Rouge | Crème | 1,8 s | Faute, puis point |
| ÉGALITÉ | — | Ardoise | Crème | 1,6 s | — |
| AVANTAGE | « Toi » ou « Adversaire », précédé de la pastille du camp | Ardoise | Crème | 1,6 s | — |
| JEU | « Toi 2 – 1 Adversaire » | Moutarde | Ardoise | 2,2 s | Jeu gagné ou perdu |
| SET | « Tu gagnes 3 – 1 » ou « L'adversaire gagne 3 – 2 » | Moutarde | Ardoise, ExtraBold 72 | 3,5 s | Set |

Quand deux messages se suivent (par exemple « DEHORS » puis « JEU »), ils s'enchaînent, jamais superposés. La zone B couvre le camp adverse : elle n'est utilisée que balle morte.

### 7.6 C. Barre de timing

Panneau de 700 × 104 en bas de l'écran. Trois lignes :

| Ligne | y dans le panneau | Contenu | Style |
|---|---|---|---|
| Résumé | 10 à 40 | Verdict du dernier swing dans une pastille de 30 px de haut, puis le résumé du coup : « Coup droit lifté · 82 km/h ». Avant la première frappe : « Frappe la balle après son rebond. » | Verdict ExtraBold 22, résumé SemiBold 22 crème |
| Piste | 48 à 70 | Piste de 620 × 22, coins de 11, centrée | Voir ci-dessous |
| Légendes | 76 à 98 | À gauche « tôt · croisé », au centre « idéal », à droite « tard · long de ligne » | Medium 20, gris clair |

Éléments de la piste. La piste représente la fenêtre de timing entière (`GameplayConfig.timingWindow`, ±0,18 s) : le centre est l'instant idéal.

| Élément | Forme | Couleur |
|---|---|---|
| Piste | Rectangle de 620 × 22 | Piste `#45585F` |
| Extrémités | Deux traits de 3 × 22 | Crème |
| Zone parfaite | Rectangle centré de 86 × 22, soit 14 % de la piste : ±25 ms | Menthe |
| Repère central | Trait de 2 × 30, dépasse de 4 px en haut et en bas | Crème |
| Curseur de la balle en approche | Barre de 8 × 30 avec un contour encre de 2 px ; traverse de gauche à droite | Crème |
| Repère du dernier swing | Barre de 6 × 34 avec un contour encre de 2 px ; fixe | Moutarde |

Verdicts : « PARFAIT » (ardoise sur menthe), « TÔT » et « TARD » (crème sur cellule), « DANS LE VIDE » (ardoise sur orange). Le verdict reste jusqu'au swing suivant.

### 7.7 D. Consignes de service

Pendant le service du joueur, le panneau du bas change de contenu, à la même place.

| Élément | Position dans le panneau | Contenu | Style |
|---|---|---|---|
| Étape 1 | x 16, y 10, 330 × 56 | Pastille « 1 », puis « Lève le téléphone : la balle part » | Pastille ronde de Ø 36 moutarde, chiffre ExtraBold 24 ardoise ; texte SemiBold 22 crème |
| Étape 2 | x 354, y 10, 330 × 56 | Pastille « 2 », puis « Frappe quand elle est en haut » | Idem |
| Jauge de lancer | x 40, y 76, 620 × 14 | Hauteur de la balle après le lancer ; zone idéale menthe sur les 18 % de droite (le sommet) | Piste, extrémités crème, curseur crème à contour encre |

L'étape en cours a un fond cellule `#31424B` et un contour moutarde de 2 px ; l'autre est à 45 % d'opacité. L'étape 1 est active avant le lancer, l'étape 2 dès que la balle est en l'air. En mode clavier et souris, le texte nomme les touches à la place des gestes. Quand l'adversaire sert, le panneau affiche une seule ligne centrée, « L'adversaire sert… », en SemiBold 24.

### 7.8 E et F. Téléphone et aide

| Élément | Contenu | Style |
|---|---|---|
| E. Téléphone connecté | Disque menthe de Ø 12, puis « Raquette connectée » | Pastille ardoise de 36 px, Medium 20 crème |
| E′. Aucun téléphone | Carte crème `#FFF4DC`, coins de 16 : code QR de 220 × 220 sur fond blanc, puis « Scanne pour connecter ta raquette » | SemiBold 20 ardoise |
| E. Erreur de liaison | Le texte de `PhoneLink.Error` | Pastille orange, Medium 20 ardoise |
| F. Aide clavier | « R recentrer · D niveau · T démo » sur deux lignes | Pastille ardoise à 72 %, Medium 18 gris clair |

Les compteurs de mise au point de l'étape c (fréquence d'échantillonnage, plus long échange) quittent l'écran de jeu. Ils restent disponibles dans les scènes de mise au point.

## 8. Sons

Tous les sons sont générés au lancement par le code, sans aucun fichier : un tableau de `float` rempli par des formules, puis `AudioClip.Create(nom, nombreÉchantillons, 1, 44100, false)` et `SetData`. Mono, 44 100 Hz.

Règles communes :

- Le bruit vient d'un `System.Random` à graine fixe (12345) : les sons sont identiques à chaque lancement.
- Filtre passe-bas à un pôle : `y += a × (x − y)` avec `a = 1 − exp(−2π × fréquence ÷ 44100)`. Passe-haut : `x − passeBas(x)`. Passe-bande : passe-haut puis passe-bas.
- « Glisse de f1 à f2, τ » : `f(t) = f2 + (f1 − f2) × exp(−t ÷ τ)`, la phase étant l'intégrale de f.
- « Lame de bois » (timbre des motifs) : `sin(2πft) + 0,35 × sin(2π × 4f × t) × exp(−t ÷ 0,025) + 0,12 × sin(2π × 10f × t) × exp(−t ÷ 0,010)`, avec une attaque linéaire de 3 ms.
- Chaque son est normalisé à 0,9 en crête et se termine par un fondu de 5 ms.
- Lecture par 6 `AudioSource` en 2D (`spatialBlend = 0`), prises à tour de rôle. Hauteur tirée à ±2 % à chaque lecture.

### 8.1 Sons demandés

| Son | Durée | Recette |
|---|---|---|
| **Frappe à plat** (« poc ») | 110 ms | Corps : sinus qui glisse de 520 à 190 Hz, τ 25 ms, enveloppe exp(−t ÷ 0,030), gain 0,9. Claquement : bruit blanc passe-haut 1 800 Hz pendant 12 ms, enveloppe exp(−t ÷ 0,004), gain 0,5. Cordage : sinus 1 240 Hz, enveloppe exp(−t ÷ 0,040), gain 0,10 |
| **Frappe liftée** | 130 ms | Frappe à plat avec un corps de 480 à 170 Hz, plus un frottement : bruit passe-bande de 2 500 à 5 000 Hz pendant 60 ms, montée de 10 ms puis exp(−t ÷ 0,020), gain 0,25 |
| **Frappe slicée** | 140 ms | Corps de 440 à 220 Hz, gain 0,7, plus un souffle : bruit passe-bas 1 200 Hz pendant 80 ms, enveloppe exp(−t ÷ 0,030), gain 0,3 |
| **Rebond** (« toc ») | 80 ms | Sinus qui glisse de 260 à 120 Hz, τ 18 ms, enveloppe exp(−t ÷ 0,020), gain 0,8. Bruit passe-bas 800 Hz pendant 6 ms, gain 0,3 |
| **Filet** (« fouf ») | 260 ms | Bruit passe-bas 700 Hz, attaque de 6 ms, enveloppe exp(−t ÷ 0,070), gain 0,7. Sinus 90 Hz, enveloppe exp(−t ÷ 0,050), gain 0,5. Trois clics de maille (bruit de 2 ms, gain 0,15) à 20, 55 et 95 ms |
| **Point gagné** | 620 ms | Lames de bois : Sol4 (392,00 Hz) à 0 ms, Do5 (523,25) à 90 ms, Mi5 (659,26) à 180 ms, Sol5 (783,99) à 270 ms. Décroissance exp(−t ÷ 0,16), exp(−t ÷ 0,30) pour la dernière. Gain 0,5 par note |
| **Point perdu** | 520 ms | Lames de bois : Mi4 (329,63 Hz) à 0 ms, Do4 (261,63) à 160 ms. Décroissance exp(−t ÷ 0,20). La seconde note descend de 40 cents en 250 ms. Passe-bas 2 500 Hz sur l'ensemble. Gain 0,4 |

Variation en jeu de la frappe et du rebond :

| Paramètre | Valeur |
|---|---|
| Volume d'une frappe du joueur | 0,55 + 0,45 × puissance du swing |
| Hauteur d'une frappe du joueur | 0,94 + 0,12 × puissance |
| Frappe de l'adversaire | Volume × 0,6, hauteur × 0,97 (il est loin) |
| Volume d'un rebond | 0,2 + 0,8 × min(1, vitesse verticale d'impact ÷ 12 m/s) ; × 0,6 dans le camp adverse |

### 8.2 Compléments

| Son | Durée | Recette |
|---|---|---|
| Faute | 220 ms | Deux coups de bois à 0 et 110 ms : sinus 880 Hz et 1 320 Hz (gain relatif 0,5), enveloppe exp(−t ÷ 0,018), gain 0,45 |
| Swing dans le vide | 180 ms | Bruit blanc, passe-bande dont la fréquence glisse linéairement de 600 à 2 400 Hz, enveloppe sin(π × t ÷ durée), gain 0,3 |
| Lancer de service | 120 ms | Sinus qui monte linéairement de 300 à 600 Hz, enveloppe sin(π × t ÷ durée), gain 0,2 |
| Bande du filet | 50 ms | Sinus 1 900 Hz et 2 850 Hz (gain relatif 0,4), enveloppe exp(−t ÷ 0,012), gain 0,4 |
| Jeu gagné | 900 ms | Motif du point gagné, puis Mi5 et Do6 (1 046,50 Hz) ensemble à 380 ms, décroissance exp(−t ÷ 0,40) |
| Jeu perdu | 800 ms | Lames de bois : Mi4, Ré4 (293,66 Hz), Do4, La3 (220,00) toutes les 150 ms, décroissance exp(−t ÷ 0,20), gain 0,4 |
| Set | 1,6 s | Lames de bois : Do5, Mi5, Sol5, Do6, Mi6 (1 318,51 Hz) toutes les 100 ms, puis l'accord Do5, Mi5, Sol5, Do6 à 600 ms, décroissance exp(−t ÷ 0,60) |

La page manette joue déjà son propre son d'impact sur l'iPhone (`controller.html`) : il est conservé, les deux sons se complètent.

## 9. Ce qui est hors de la v1

- Textures peintes, modèles importés, animations par squelette.
- Spectateurs, arbitre, ramasseurs de balles.
- Plusieurs courts ou plusieurs heures de la journée.
- Musique.

## 9 bis. Variante retenue à l'implémentation : « Stade de jour »

Choix de l'utilisateur le 2026-11-16, pendant l'implémentation : un stade en plein jour plutôt que le soir des lampions. C'est l'ambiance par défaut de la scène `Court`. Le soir décrit plus haut reste disponible : champ `Ambiance` du composant `MatchVisuals` (objet `Jeu`), valeurs `Jour` et `Soir`, avec le champ `Surface` (`Gazon` ou `Sarcelle`).

L'utilisateur a demandé une copie de l'apparence d'un jeu de tennis familial sur console. Cette copie a été refusée. Le stade de jour ne reprend que des idées générales (un stade autour du court, du gazon, un ciel bleu, le score en haut au centre) dans un dessin propre : aucun personnage, décor, logo ou élément d'interface de ce jeu n'est repris, et les murs ne sont pas verts.

Ce qui change par rapport aux sections 2 à 7 :

| Élément | Soir des lampions | Stade de jour |
|---|---|---|
| Surface | Sarcelle | Gazon tondu (section 2.6) |
| Abords | Brique | Brique (inchangé : le gazon n'est pas posé sur du vert) |
| Côtés | Haies, gradins à gauche, parasols à droite | Une tribune en bois de 5 marches et 36 m de chaque côté, sièges de couleur, mur de fond en bois foncé |
| Fond | Haie, puis rideau de peupliers | Haie (fond sombre de la balle), puis tribune de 6 marches en bois foncé `#7A4E2D` et `#5E3D26`, sur un socle |
| Arbres | 17 peupliers, 5 tilleuls | 11 tilleuls et 4 peupliers derrière les tribunes : le ciel se voit entre eux |
| Lampions | Émission 0,9 | Émission 0,25 ; poteaux reculés à x = ±13,5, derrière les tribunes |
| Pré | `#6E8C4A` | `#5C8F52` |
| Ciel | `#FFD9A8` vers `#9CBFD4` | `#CFE8F5` vers `#5BA8E0`, nuages blancs, 5 groupes |
| Brume | `#F3CBA4` | `#D5E8F0` |
| Soleil | Euler (38 ; 28 ; 0), `#FFE3BD` | Euler (50 ; 28 ; 0), `#FFF4E0` |
| Ardoise de score | Haut gauche | Haut centre (dans les deux ambiances) |

Les sièges sont tirés avec la graine 7, par groupes de quatre de même couleur, une place sur neuf vide. La chaise d'arbitre, les collines, le filet, les personnages, la balle, le HUD et les sons sont communs aux deux ambiances.

**Personnages redessinés (demande de l'utilisateur, même jour).** Les figurines en bois de la section 4 n'ont pas plu ; l'utilisateur voulait des personnages proches de ceux du jeu de référence. Ces personnages-là ne sont pas repris. À la place, un personnage de dessin animé original, toujours en formes simples (`CharacterModel`) :

| Pièce | Primitive | Position locale | Échelle | Couleur |
|---|---|---|---|---|
| Tête | Sphère | (0 ; 1,34 ; 0) | (0,44 ; 0,42 ; 0,44) | Peau |
| Cheveux | Sphère | (0 ; 1,39 ; −0,04) | (0,47 ; 0,40 ; 0,44) | Brun `#5A3F2C` (joueur), prune nuit (adversaire) |
| Yeux (2) | Sphère | (±0,085 ; 1,36 ; 0,196) | (0,055 ; 0,075 ; 0,03) | Encre |
| Joues (2) | Sphère | (±0,135 ; 1,29 ; 0,162) | (0,07 ; 0,045 ; 0,03) | Rose |
| Torse | Capsule | (0 ; 0,86 ; 0) | (0,40 ; 0,27 ; 0,31) | Maillot |
| Bande du maillot | Cylindre | (0 ; 0,90 ; 0) | (0,415 ; 0,055 ; 0,322) | Couleur du camp |
| Short | Sphère | (0 ; 0,60 ; 0) | (0,40 ; 0,24 ; 0,32) | Couleur du camp |
| Jambes (2) | Capsule | (±0,10 ; 0,31 ; 0) | (0,13 ; 0,26 ; 0,13) | Peau |
| Chaussures (2) | Sphère | (±0,10 ; 0,05 ; 0,04) | (0,15 ; 0,10 ; 0,27) | Crème |
| Bras (bras et avant-bras) | Capsule | De l'épaule (±0,20 ; 1,02 ; 0) au coude, puis à la main | Ø 8,5 cm | Peau |
| Mains (2) | Sphère | Au bout du bras | Ø 15 cm | Peau |

Ce qui reste de la section 4 : les bras reliés au corps (pas de mains détachées), les couleurs des camps, la casquette à l'envers du joueur, le bandeau de l'adversaire, les trois mouvements par le code. Le bras de la raquette a un coude : en revers il passe devant le torse au lieu de le traverser. Racine à l'échelle 1,08 (joueur) et 1,14 (adversaire). Peau : `#E2B88A` (joueur), `#C68E5F` (adversaire).

**Public (demande de l'utilisateur).** Deux sièges sur trois des tribunes du stade de jour sont occupés par un spectateur assis, dans le même style, en trois formes : corps (capsule), tête et cheveux (sphères). Habits tirés dans la palette (jamais de jaune ni de vert), cinq teintes de peau, six couleurs de cheveux, graine 7. Le public sautille pendant 1,4 s à la fin de chaque point (`DecorAnimator.Cheer`). Les matériaux partagés activent l'instanciation GPU : environ 2 000 objets pour 400 appels de dessin. Le soir des lampions reste sans public, comme dans la section 1.

**HUD : écarts avec la section 7.**

- L'ardoise de score est en haut au centre, son étiquette de service juste dessous. Les annonces descendent donc à y = 208 (sous-titre à y = 308). Un titre ou un sous-titre trop long est écrit plus petit (jamais sous 18 px) ; le sous-titre ne dépasse pas 700 px de large.
- L'aide est en haut à gauche (330 px de large) : en bas à gauche, elle passait sous la barre de timing.
- Toutes les informations de l'ancien HUD sont gardées, y compris celles que la section 7.8 retirait : la cadence des capteurs (dans la pastille « Raquette connectée ») et le nombre de frappes de l'échange (pastille en bas à gauche). Le résumé du swing garde l'écart en millisecondes.
- Les coins arrondis sont dessinés par `GUI.DrawTexture` (paramètre `borderRadius`), sans texture à neuf tranches.
- Police Baloo 2 non importée : le projet n'admet que des primitives et des matériaux Unity, et aucun fichier tiers n'a été téléchargé. `HudSkin` charge les trois fichiers de la section 7.2 s'ils sont posés dans `Resources/Fonts/` ; sinon la police par défaut d'Unity est utilisée, en gras pour les deux graisses fortes.
- Étapes du service en SemiBold 20 sur deux lignes : en 22 px sur une ligne, les textes ne tiennent pas dans 330 px.

**Sons : écarts avec la section 8.** Un set perdu joue le motif « jeu perdu », pas la montée du set. Une balle dehors joue « faute » sans le motif du point ; une balle dans le filet ne joue que le son du filet. La bande du filet sonne quand la balle touche à moins de 15 cm du haut. Volume général réglable sur `SoundPlayer` (0,8 par défaut).

**Non fait.** Tâche 13 (page manette aux couleurs de la palette) : une autre session modifiait `controller.html` au même moment.

**Éclairage corrigé (les deux ambiances).** Les valeurs de la section 5.7 surexposaient la palette : le shader `Standard` additionne le soleil et la lumière d'ambiance, et le sol recevait 1,5 fois sa couleur (court turquoise vif au lieu de sarcelle). Valeurs retenues, pour que la somme reçue par le sol reste proche de 1 :

| Réglage | Soir | Jour |
|---|---|---|
| Intensité du soleil | 0,85 | 0,9 |
| `ambientSkyColor` | `#657585` | `#5C6B80` |
| `ambientEquatorColor` | `#806E5B` | `#6E7478` |
| `ambientGroundColor` | `#4B3B32` | `#4A4A40` |

La portée des ombres (70 m, 2 cascades) est réglée au lancement par `Atmosphere.ApplyShadowQuality`, pas dans `SceneBuilder` : elle vaut ainsi pour tous les niveaux de qualité.

## 10. Tâches d'implémentation, dans l'ordre conseillé

À commencer après le commit de l'étape e : les tâches 5, 7, 8 et 12 touchent `RallyController.cs` et l'affichage, que cette étape modifie en ce moment. Après chaque tâche : lancer le jeu, lire la console, corriger.

| N° | Fichier | Tâche | Section |
|---|---|---|---|
| 1 | `Assets/TennisPhone/Scripts/Art/Palette.cs` (nouveau) | Classe statique : toutes les couleurs de la section 2 à partir de leur code hexadécimal, l'énumération `Surface`, et un cache de matériaux partagés par recette et par couleur (`Mat`, `Lumineux`, `Voile`) | 2, 3 |
| 2 | `Assets/TennisPhone/Scripts/Court/CourtBuilder.cs` | Remplacer les six constantes de couleur par `Palette`. Faire utiliser à `Primitive` un matériau partagé (`sharedMaterial`) au lieu de `renderer.material`. Poteaux en bois foncé, boules crème | 2.1, 5.2 |
| 3 | `Assets/TennisPhone/Editor/SceneBuilder.cs` | Dans `BuildCourtScene` : fond et profondeur de la caméra, soleil, lumière d'ambiance, brume, portée des ombres, matériaux modèles `Lumineux.mat` et `Voile.mat`. Recréer ensuite la scène par le menu « TennisPhone/Creer la scene du court » | 3, 5.7 |
| 4 | `Assets/TennisPhone/Scripts/Court/DecorBuilder.cs` (nouveau) | `DecorBuilder.Build(court)` : pré, haies, peupliers, tilleuls, gradins et coussins, parasols, chaise d'arbitre, lampions, collines, nuages, ciel. Appelé dans `RallyController.Start` après `CourtBuilder.Build` | 5.3 à 5.6 |
| 5 | `Assets/TennisPhone/Scripts/Player/CharacterModel.cs` (nouveau), puis `RallyController.cs` | Construire la figurine (pièces communes, coiffe et couleurs par camp) et une méthode qui tend les bras vers la main. Remplacer `BuildCharacter` et appeler la pose dans `UpdateVisuals` | 4 |
| 6 | `Assets/TennisPhone/Scripts/Player/RacketModel.cs` | `Build` reçoit la couleur du camp ; nouvelles couleurs du manche et du cordage. Conserver le `Renderer` du cordage, aujourd'hui ignoré dans `BuildActors` | 4.5 |
| 7 | `Assets/TennisPhone/Scripts/Game/RallyController.cs` (`BuildActors`, `UpdateVisuals`, `ShowLanding`) | Balle en recette Lumineux avec sa bande et sa rotation ; ombre en recette Voile, taille et opacité selon la hauteur ; `TrailRenderer` ; marque ronde ou croix. Exposer des événements : frappe (camp, puissance, effet, point d'impact), rebond, filet, swing dans le vide, fin de point | 6.1 à 6.3 |
| 8 | `Assets/TennisPhone/Scripts/Game/HitEffects.cs` (nouveau) | Abonné aux événements de la tâche 7 : éclair du cordage, anneau d'impact, éclats de la frappe parfaite, recul de caméra, balle morte, clignotement de ligne, recul du filet | 6.4 |
| 9 | `Assets/TennisPhone/Scripts/Audio/SoundSynth.cs` (nouveau) | Génération des clips des sections 8.1 et 8.2 | 8 |
| 10 | `Assets/TennisPhone/Scripts/Audio/SoundPlayer.cs` (nouveau) | Six `AudioSource` ; abonné aux événements de la tâche 7 ; volumes et hauteurs selon la puissance et le camp | 8.1 |
| 11 | `Assets/TennisPhone/Resources/Fonts/` (nouveau) | Importer `Baloo2-Medium.ttf`, `Baloo2-SemiBold.ttf`, `Baloo2-ExtraBold.ttf` et `OFL.txt` ; vérifier les glyphes accentués | 7.2 |
| 12 | `Assets/TennisPhone/Scripts/UI/HudSkin.cs` (nouveau), puis le HUD de l'étape e (`RallyHud.cs` ou son remplaçant) | `HudSkin` : polices, styles, texture de panneau arrondi, dessin d'une pastille. Puis, zone par zone : ardoise de score, messages, barre de timing, consignes de service, téléphone et aide | 7 |
| 13 | `Assets/TennisPhone/Resources/controller.html` (facultatif) | Accorder la page manette à la palette : fond `#1F2A30`, texte `#FFF4DC`, bouton « Activer la raquette » moutarde `#FFC53D` à texte ardoise, flash crème | 2.5 |
| 14 | — | Vérification finale : lancer la scène `Court`, console sans erreur ; captures en 16:9 et en 21:9 ; contrôler les cinq règles de lisibilité de la section 1 ; jouer un jeu complet au clavier puis au téléphone ; commit et push | 1 |
