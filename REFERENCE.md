# REFERENCE — règles et constantes de gameplay observées

Document de la phase REA. Il recense ce que deux jeux de tennis de référence font réellement, pour régler notre propre jeu. **Aucun code ni asset n'est repris** : seules des règles et des valeurs numériques sont relevées.

Date du relevé : 2026-11-16.

## Étiquettes

- **observé** : lu directement dans le code source ou dans le bundle analysé, valeur exacte.
- **déduit** : calculé ou interprété à partir de valeurs observées (conversion d'unités, lecture d'une formule). Peut être faux si l'interprétation l'est.
- **inconnu** : cherché, non trouvé ou non vérifié.

## Sources

| Id | Source | Méthode | Preuve |
|---|---|---|---|
| MT | MinimumTennis (Unity, licence MIT) | Lecture statique du code C#, jeu non exécuté | Dépôt `open-video-game-library/MinimumTennis`, commit `af08642` du 2025-04-14 |
| TT | tetetennis (jeu web, aucune licence visible) | REA : capture passive de la démo en ligne (`inspect_web_page`), export du script (`export_web_scripts`), lecture statique du bundle, jeu non joué | Bundle `assets/index-BAuDA0FU.js`, sha256 `a438ef50…6ba38`, preuves REA `ev_337302c9…` et `ev_543b4033…` |
| ITF | Règles du tennis de l'ITF | Lecture du texte de la règle 1 | Texte « ITF Rules of Tennis », règle 1 « The Court » |

Unités : MT utilise ses propres unités. Le demi-court y mesure 47,54 unités pour 11,885 m réels, soit **4 unités par mètre** (déduit). Toutes les conversions en mètres de MT ci-dessous reposent sur ce facteur. TT travaille directement en mètres et en secondes.

## 1. Court et filet

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Longueur du court | 23,77 m | observé | ITF |
| Largeur en simple | 8,23 m | observé | ITF |
| Hauteur du filet au centre | 0,914 m | observé | ITF |
| Hauteur du filet aux poteaux | 1,07 m | observé | ITF |
| Demi-longueur, demi-largeur, ligne de service | 11,885 m, 4,115 m, 6,4 m du filet | observé | TT (constantes `Ge`, `Rn`, `Fi`) |
| Court de MT | ±16,8 × ±47,54 unités, soit 8,4 × 23,77 m | observé, conversion déduite | MT `GameData.cs:74` |
| Filet de MT | 4,0 unités, soit 1,0 m, hauteur uniforme | observé, conversion déduite | MT `GameData.cs:76` |
| Zone de déplacement du joueur (MT) | x ±32, z de 10 à 80 unités : jusqu'à 8 m de l'axe et 8 m derrière la ligne de fond, jamais à moins de 2,5 m du filet | observé, conversion déduite | MT `GameData.cs:79` |

## 2. Balle : vol

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Gravité | 9,81 m/s² | observé | TT |
| Gravité (MT) | 49 unités/s², soit 12,25 m/s² (1,25 g) | observé, conversion déduite | MT `NormalBallController.cs:30` |
| Modèle de vol (MT) | Parabole pure : vitesse horizontale constante, ni traînée ni effet | observé | MT `NormalBallController.cs:46-56` |
| Rayon de la balle | 0,033 m | observé | TT (constante `ni`) |
| Pas d'intégration | 1/120 s | observé | TT |
| Traînée | accélération = −k · Cd · ‖v‖ · v, avec k = 0,036313 m⁻¹ et Cd = 0,311 | observé | TT (`aero_cd`) |
| Masse de la balle | environ 57,7 g, en supposant k = ρ·π·r²/(2m) avec ρ = 1,225 kg/m³ | déduit | TT |
| Effet Magnus | accélération = k · Cl · ‖v‖ · (ω × v)/‖ω‖, Cl = min(1/(2 + 1/S), 0,357), S = r·‖ω‖/‖v‖ | observé | TT (`aero_cl_max`) |
| Décroissance de la rotation | exponentielle, constante de temps 7,44 s | observé | TT (`aero_spin_decay_tau`) |
| Rotation en lift | de 1 863 à 3 964 tr/min (base + pente) | observé, borne haute déduite | TT (`topspin_rpm_base`, `topspin_rpm_slope`) |
| Rotation en slice | de 1 500 à 3 300 tr/min | observé, borne haute déduite | TT (`slice_rpm_base`, `slice_rpm_slope`) |

## 3. Balle : rebond

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Restitution verticale (MT) | 0,70, constante | observé | MT `NormalBallController.cs:125` |
| Frottement au rebond (MT) | Aucun : la vitesse horizontale est conservée | observé | MT |
| Seuil de rebond (MT) | Hauteur prévue à l'image suivante inférieure à 0,20 unité (5 cm) | observé | MT `NormalBallController.cs:58` |
| Restitution et frottement par surface (TT) | Terre battue 0,79 / 0,80 ; dur 0,74 / 0,56 ; gazon 0,64 / 0,38 | observé | TT (objets `clay`, `hard`, `grass`, champs `ey` et `mu`) |
| Restitution qui baisse avec la vitesse d'impact (TT) | Paramètres : vitesse de référence 7,66 m/s, pente 0,0146, plancher 0,648 | observé pour les valeurs, **inconnu** pour la formule exacte | TT (`bounce_v_ref`, `bounce_slope`, `bounce_min_frac`) |
| Influence de la rotation sur le rebond vertical (TT) | Coefficient 0,00628 | observé pour la valeur, **inconnu** pour la formule | TT (`spin_bounce_vertical`) |
| Balle dans le filet (MT) | La vitesse en profondeur est inversée et réduite à 10 % | observé | MT `NormalBallController.cs:94` |

## 4. Coups : vitesses initiales

Dans MT, chaque type de coup impose une vitesse initiale fixe. Conversion en m/s déduite (4 unités par mètre).

| Coup (MT) | Vitesse verticale | Vitesse en profondeur | Source |
|---|---|---|---|
| Normal | 25 u/s (6,25 m/s) | 65 u/s (16,25 m/s, 58 km/h) | `Shot.cs:88-90` |
| Lob | 45 u/s (11,25 m/s) | 45 u/s (11,25 m/s) | `Shot.cs:126-128` |
| Rapide | 14 u/s (3,5 m/s) | 100 u/s (25 m/s, 90 km/h) | `Shot.cs:164-166` |
| Amorti | 25 u/s (6,25 m/s) | 48 u/s (12 m/s) | `Shot.cs:203-205` |
| Smash | −20 u/s (−5 m/s) | 120 u/s (30 m/s, 108 km/h) | `Shot.cs:241-243` |
| Volée | 5 u/s (1,25 m/s) | 55 u/s (13,75 m/s) | `Shot.cs:279-281` |
| Service | −6 u/s (−1,5 m/s) | 100 u/s (25 m/s), plus 25 u/s en latéral vers le carré opposé | `Shot.cs:69-79` |

Autres valeurs de frappe :

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Vitesse d'un coup à plat (TT) | Base 18 m/s, pente 7 m/s selon la statistique du joueur | observé | TT (`flat_speed_base`, `flat_speed_slope`) |
| Multiplicateur de vitesse par type (TT) | Plat 1,04 ; lift 0,97 ; slice 0,87 | observé | TT |
| Angle de départ (TT) | Plat : jusqu'à 15,3° ; lift : de 10,9° à 27,8° | observé | TT (`flat_theta_max`, `topspin_theta_*`) |
| Marge au-dessus du filet (TT) | 0,05 m en frappe appuyée, 0,20 m en frappe contrôlée | observé | TT (`net_margin_drive`, `net_margin_control`) |
| Vitesse de service (TT) | De 27 à 48,6 m/s (97 à 175 km/h) | observé | TT (`serve_speed_min`, `serve_speed_max`) |
| Smash ou volée (MT) | Seulement dans la moitié avant du demi-court : smash si la balle est au-dessus de 10 unités (2,5 m), volée au-dessus de 2,5 unités (0,63 m) | observé, conversion déduite | MT `PlayerNormalController.cs` |
| Aide anti-faute (MT) | Si le point de chute prévu dépasse une ligne de moins de 10 unités (2,5 m), la vitesse est recalculée pour viser 3 unités (0,75 m) à l'intérieur | observé | MT `Shot.cs:389-426` |
| Aide au passage du filet (MT) | Si la balle arriverait entre la mi-hauteur et le haut du filet, la profondeur est réduite à 60 % et la trajectoire relevée pour passer à 7 unités (1,75 m) | observé | MT `Shot.cs:364-387` |

## 5. Swing : fenêtre de timing et direction

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Fenêtre de contact (TT) | Le contact est tenté de 0,09 s à 0,27 s après le début du swing, soit une fenêtre de 0,18 s | observé | TT |
| Swing manqué (TT) | Sans contact à 0,27 s, le coup est raté et le joueur est bloqué 0,25 s | observé | TT (`whiff_cd`) |
| Contact du joueur CPU (TT) | 0,18 s après le début de son swing | observé | TT |
| Puissance selon la charge (TT) | Multiplicateur 1 + 0,5 × charge (charge de 0 à 1) | observé | TT |
| Fenêtre de frappe (MT) | Spatiale : le coup part si la balle est dans la zone de frappe du joueur pendant qu'un swing est actif | observé | MT `PlayerNormalController.cs`, `OnTriggerStay` |
| Taille de la zone de frappe (MT) | Définie dans les prefabs des personnages, non lue | **inconnu** | — |
| Direction selon le timing (MT) | Vitesse latérale = facteur × écart en profondeur entre la balle et le joueur. Balle frappée tôt (devant le joueur) : croisé. Frappée tard (derrière) : côté opposé | observé | MT `Shot.cs:110-113` |
| Facteur de direction (MT) | 2,0 par unité d'écart en coup normal (2,5 rapide, 1,25 lob, 4,0 smash) | observé | MT `Shot.cs` |
| Effet pratique du facteur (MT) | En coup normal, 1 m d'écart de profondeur déplace le point de chute d'environ 2,3 m en latéral (temps de vol d'environ 1,16 s) | déduit | MT |
| Coup droit ou revers (MT) | Choisi par le côté où se trouve la balle par rapport au joueur, puis croisé avec la main dominante | observé | MT `Shot.cs:96-105` |
| Erreur de frappe (TT) | Dispersion latérale 0,58 m, en profondeur 1,18 m, amplifiée quand la qualité du coup baisse | observé pour les valeurs, **inconnu** pour la loi exacte | TT (`stroke_err_x`, `stroke_err_z`, `err_quality_gain`) |

### Détection du swing par capteur de mouvement (MT, manette à accéléromètre)

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Signal utilisé | Un seul axe de l'accéléromètre (y), multiplié par un seuil réglable (1,0 par défaut). Le gyroscope n'est pas utilisé | observé | MT `JoyconInputManager.cs:145-158` |
| Déclenchement | Accélération y inférieure à −1,80 | observé ; unité en g **déduite** | MT `JoyconInputManager.cs:153` |
| Durée de mesure après déclenchement | 10 pas de physique, soit 0,2 s si le pas est de 0,02 s | observé ; durée déduite (pas par défaut d'Unity) | MT `JoyconInputManager.cs:34` |
| Délai avant un nouveau swing | 60 pas (1,2 s) la première fois, puis 120 pas (2,4 s) | observé ; durées déduites | MT `JoyconInputManager.cs:38, 204` |
| Intensité du swing | Le pic d'accélération est mesuré mais ne modifie pas la puissance du coup | observé | MT |
| Détection du côté opposé | La condition exige une valeur à la fois supérieure à 1,0 et inférieure à −2,50 : elle ne peut jamais être vraie | observé | MT `JoyconInputManager.cs:147` |

## 6. Service

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Lancer (MT) | Balle créée à 6 unités de haut (1,5 m) et 5 unités devant le joueur (1,25 m), vitesse verticale 28 u/s (7 m/s) | observé, conversion déduite | MT `Shot.cs:44-63` |
| Sommet du lancer (MT) | 14 unités (3,5 m), atteint 0,57 s après le lancer | déduit | MT |
| Moment de frappe du CPU (MT) | Quand la balle atteint 14 unités, c'est-à-dire au sommet | observé | MT `PlayerNormalAI.cs:200` |
| Lancer raté (MT) | Si la balle redescend sous 5 unités (1,25 m) sans être frappée, elle disparaît et le lancer est à refaire | observé | MT `NormalBallController.cs:67-71` |
| Délai du CPU avant de lancer (MT) | 2,0 s | observé | MT `PlayerNormalAI.cs:184` |
| Lancer (TT) | Balle lâchée à 1,7 m, sommet entre 2,55 et 3,10 m selon la statistique du joueur | observé pour la formule, sommet déduit | TT |
| Jauge de service (TT) | Zone idéale entre 0,70 et 0,88 de la jauge | observé | TT (`serve_sweet_lo`, `serve_sweet_hi`) |
| Position du serveur (MT) | À 8 unités (2 m) de l'axe, du côté qui alterne à chaque point | observé | MT `PrepareManager.cs:62-72` |

## 7. Déplacement du joueur

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Vitesse maximale (MT) | 30 u/s, soit 7,5 m/s | observé, conversion déduite | MT `Parameters.cs`, `Move.cs` |
| Accélération (MT) | +1 u/s par image jusqu'à la vitesse maximale | observé | MT `Move.cs` |
| Freinage (MT) | Vitesse multipliée par 0,90 à chaque image sans commande | observé | MT `Move.cs` |
| Échelle de vitesse de course (TT) | 0,859 | observé pour la valeur, vitesse de base **inconnue** | TT (`run_speed_scale`) |

## 8. IA adverse

### MinimumTennis

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Temps de réaction | 0,30 s après la frappe adverse avant de se déplacer | observé | MT `PlayerNormalAI.cs:8, 238` |
| Anticipation | Calcule le point de chute par la formule de la parabole, puis vise ce point + 0,30 × la vitesse de la balle, donc derrière le rebond | observé | MT `PlayerNormalAI.cs:256-278` |
| Décision de jouer la balle | Seulement si le point de chute est dans son demi-court, avec 5 unités (1,25 m) de marge | observé | MT `PlayerNormalAI.cs:234` |
| Tolérance d'arrivée | Considère être en place à ±4 unités (1 m) de la cible | observé | MT `PlayerNormalAI.cs:145` |
| Replacement après sa frappe | Après 0,45 s, retourne à 4 unités (1 m) de l'axe, du côté opposé à sa balle, et à 52 unités du filet (13 m, soit 1,1 m derrière la ligne de fond) | observé, conversion déduite | MT `PlayerNormalAI.cs:249, 280-289` |
| Cible visée | 6 unités (1,5 m) de l'axe, du côté opposé au joueur adverse dans 3 cas sur 4 ; profondeur 40 unités (10 m), ou 35 unités si l'adversaire est au filet | observé, proportion déduite | MT `PlayerNormalAI.cs:291-352` |
| Hauteur des coups | Vitesse verticale tirée entre 20 et 30 u/s en échange normal, entre 45 et 50 (lob) si l'adversaire est au filet, entre 30 et 40 si l'IA court encore | observé | MT `PlayerNormalAI.cs:319-338` |
| Erreurs | Si l'IA n'est pas arrivée en place : visée latérale multipliée par un facteur entre −0,5 et 1, profondeur entre 0,8 et 1. Si elle est en place : même erreur dans 20 % des coups | observé | MT `PlayerNormalAI.cs:365-380` |
| Direction du service | Aléatoire parmi gauche, centre, droite | observé | MT `PlayerNormalAI.cs:205` |

### tetetennis

Trois niveaux de difficulté. Les valeurs brutes sont observées ; les valeurs effectives appliquent les échelles globales `ai_react_scale` = 1,518 et `ai_pos_err_scale` = 1,160 (déduit).

| Paramètre | Facile | Normal | Difficile | Étiquette |
|---|---|---|---|---|
| Temps de réaction brut | 0,55 s | 0,22 s | 0,06 s | observé |
| Temps de réaction effectif | 0,83 s | 0,33 s | 0,09 s | déduit |
| Erreur de lecture du point de chute (brute) | 2 | 1 | 0,35 | observé ; unité **inconnue** |
| Tremblement de placement | 2 | 1 | 0,5 | observé ; unité **inconnue** |
| Multiplicateur de vitesse de course | 0,7 | 1,0 | 1,1 | observé |
| Bonus de qualité de service | −0,10 | 0 | +0,08 | observé |
| Bruit sur le choix du coup | 0,45 | 0,25 | 0,12 | observé |

La façon exacte dont ces paramètres entrent dans les décisions du CPU n'a pas été reconstituée (**inconnu**).

## 9. Score et fautes

| Élément | Valeur | Étiquette | Source |
|---|---|---|---|
| Points d'un jeu | 0, 15, 30, 40 | observé | TT (tableau `Pu`) |
| Égalité | Dès que les deux joueurs ont au moins 3 points, il faut 2 points d'écart | observé | MT `PlayingManager.cs:140` |
| Longueur du match (MT) | Réglable : premier à 1, 2, 3, 4 ou 5 jeux | observé | MT `Parameters.cs` (`GameSize`) |
| Longueur du set (TT) | Réglable : 2, 4 ou 6 jeux | observé pour les valeurs, règle d'écart **inconnue** | TT (tableau `ys`) |
| Faute de service | Première faute : second service. Deuxième faute : double faute, point perdu | observé | MT `PlayingManager.cs:38-47` |
| Côté de service | Alterne à chaque point selon la parité du total de points du jeu | observé | MT `PrepareManager.cs:62-63` |
| Point perdu | Balle dehors au premier rebond, balle dans le filet ou dans son propre camp, deux rebonds avant la frappe | observé | MT `PlayingManager.cs`, `NormalBallController.cs:98-121` |
| Changement de serveur à chaque jeu | Non vérifié dans le code | **inconnu** | — |

## 10. Ce qui reste inconnu

- La taille de la zone de frappe du joueur dans MT (dans les prefabs, non lus).
- Les formules exactes du rebond dans TT : les paramètres sont connus, pas leur combinaison.
- Le détail des décisions du CPU dans TT.
- La signification de deux paramètres de réglage du bundle TT dont le nom n'a pas pu être retrouvé (valeurs 9,2 et 1).
- Le comportement réel en jeu : les deux relevés sont statiques. Aucune partie n'a été jouée ni mesurée.
- Les spécifications officielles de la balle (masse, rebond) : non vérifiées dans le texte ITF.

## 11. Ce que ces références ne couvrent pas

Aucune des deux références ne correspond exactement à notre manette :

- Aucune n'utilise le gyroscope, ni la rotation du poignet pour choisir lift ou slice. MT choisit le type de coup par bouton, TT par touche.
- Aucune ne relie l'intensité du geste à la puissance : MT ignore le pic mesuré, TT utilise une durée de charge.
- Le timing du swing oriente la balle dans MT par un écart de position, pas par un écart de temps.

Les seuils de détection du swing au téléphone (pic d'accélération, vitesse angulaire, fenêtre temporelle) devront donc être réglés sur des mesures réelles de l'iPhone à l'étape b.

## 12. Valeurs de départ proposées pour `GameplayConfig`

Proposition de réglage initial, à ajuster en jouant. Tout ce tableau est **déduit** : ce sont nos choix, inspirés des valeurs ci-dessus.

| Paramètre | Valeur de départ | Inspiré de |
|---|---|---|
| Court | 23,77 × 8,23 m, filet 0,914 m au centre et 1,07 m aux poteaux | ITF |
| Gravité | 9,81 m/s² | TT |
| Traînée et effet | Modèle TT : Cd 0,31, Cl max 0,36, pas de 1/120 s | TT |
| Restitution et frottement au rebond | 0,74 et 0,56 (surface dure) | TT |
| Vitesse d'un coup normal | 16 à 25 m/s selon l'intensité du swing | MT (16,25 à 25 m/s) |
| Vitesse de service | 25 à 40 m/s | MT et TT |
| Rotation lift / slice | Jusqu'à 3 000 tr/min selon la rotation du poignet | TT |
| Fenêtre de timing du swing | ±0,18 s autour de l'instant de contact idéal | TT (fenêtre de 0,18 s) |
| Direction selon le timing | Écart latéral maximal de 3 m au point de chute pour un swing en bord de fenêtre | MT (2,3 m par mètre d'écart) |
| Blocage après un swing manqué | 0,25 s | TT |
| Aide anti-faute | Ramène à 0,75 m dans le court une balle qui sortirait de moins de 2,5 m | MT |
| Vitesse de déplacement des joueurs | 7,5 m/s | MT |
| Temps de réaction de l'IA | 0,8 s facile, 0,33 s normal, 0,1 s difficile | TT |
| Erreur de l'IA | 20 % de coups imprécis quand elle est en place, davantage en course | MT |
| Lancer de service | Sommet à 3 m, frappe idéale au sommet | MT et TT |
| Set | Premier à 3 jeux | Cadrage du projet |
