# TennisPhone

Jeu de tennis Unity, solo contre une IA, vue derrière le joueur. L'iPhone sert de raquette : il ouvre une page web servie par Unity et envoie ses capteurs par WebSocket.

## Cadrage validé (2026-11-16)

- **Moteur** : Unity 6000.6.9f1. Pilotage de l'éditeur par MCP for Unity (CoplayDev).
- **Référence de gameplay** : MinimumTennis (MIT, lecture du code C#) + tetetennis (sans licence : relever des valeurs uniquement, via REA sur la démo web) + règles officielles du tennis. Résultat dans `REFERENCE.md`, chaque élément étiqueté « observé », « déduit » ou « inconnu » avec sa source. Aucun code ni asset repris.
- **Téléphone** : iPhone (Safari). Connexion directe en Wi-Fi en HTTPS/WSS avec un certificat local (profil à installer et approuver une fois sur l'iPhone). Pas de tunnel.
- **Page manette** : bouton « Activer la raquette » (permission DeviceMotion iOS déclenchée par un clic), envoi DeviceMotion + DeviceOrientation à 60 Hz.
- **Vibration** : Safari iOS n'a pas d'API de vibration. Tenter un contournement, avec son + flash d'écran en repli.
- **Joueur** : déplacement automatique, le joueur ne gère que le swing. Droitier par défaut, réglable.
- **Swing** : pic d'accélération + vitesse angulaire. Timing → direction (tôt = croisé, tard = long de ligne), intensité → puissance, geste du poignet → lift ou slice (voir « État du projet » pour ce que les mesures ont montré).
- **Service** : deux gestes (lever le téléphone lance la balle, un swing la frappe).
- **Score** : 15/30/40, égalité, avantage. Set au premier à 3 jeux, réglable.

## Contraintes

- Assets originaux ou libres de droits : formes simples et matériaux Unity. Aucun nom, sprite, son ou personnage de Nintendo.
- Toutes les constantes de gameplay dans un ScriptableObject (`GameplayConfig`).
- Mode clavier/souris pour tester sans téléphone.
- Les clés privées de certificat (`/Certs/`) ne sont jamais versionnées.

## Méthode

Étapes dans cet ordre, chacune testable :

1. Phase REA : `REFERENCE.md`.
2. a. Connexion téléphone → Unity avec écran de debug des capteurs.
3. b. Détection du swing sur une raquette seule.
4. c. Balle, physique, filet, limites du court.
5. d. IA adverse.
6. e. Score, service, HUD.

Après chaque étape : lancer le jeu, lire la console, corriger, puis commit Git et push vers le dépôt GitHub privé.

## État du projet (2026-11-16)

Les étapes REA et a à e sont faites. Le jeu complet se joue dans `Assets/TennisPhone/Scenes/Court.unity`.

### Ce que les mesures réelles ont changé par rapport au cadrage

Deux enregistrements des capteurs de l'iPhone du joueur ont servi à régler la détection :

- **Lift** : rotation rapide du poignet autour du manche (plus de 250 °/s en moyenne pendant le geste).
- **Slice** : le poignet tourne dans le même sens que pour le lift. Ce qui le distingue : l'écran du téléphone finit tourné vers le ciel après la frappe. Il est donc reconnu juste après l'impact (`SwingRefined`, même `Id` que le swing), et la trajectoire de la balle est corrigée en vol.
- **Lancer du service** : le geste est trop lent pour le détecteur de swing. Il est reconnu par l'inclinaison : téléphone tenu bas, puis levé au-dessus de 55° (`TossDetector`).
- **Vibration** : rien sur iPhone malgré deux contournements. Le retour à l'impact est un son et un flash d'écran.

### Scènes

- `Court.unity` : le match (service, échange, score, IA).
- `SwingLab.unity` : raquette seule, jauges du swing, enregistreur de capteurs (touche G : enregistrement guidé).
- `SensorDebug.unity` : connexion du téléphone et capteurs bruts.

Les scènes sont construites par le code : menu `TennisPhone` de l'éditeur (`Assets/TennisPhone/Editor/SceneBuilder.cs`). Tout le décor et les personnages sont créés au lancement, en formes simples.

### Habillage (ART_DIRECTION.md)

La charte est dans `ART_DIRECTION.md` ; sa section 9 bis dit ce qui a été retenu à l'implémentation.

- **Deux ambiances**, champ `Ambiance` du composant `MatchVisuals` (objet `Jeu`) : `Jour` (stade en bois avec public, gazon, ciel bleu ; par défaut) et `Soir` (soir des lampions, court sarcelle). Champ `Surface` : `Gazon` ou `Sarcelle`.
- `RallyController` ne dessine rien. Il expose des événements (frappe, rebond, filet, swing dans le vide, lancer, fin de point, annonces) ; `MatchVisuals` (court, décor, personnages, balle, effets), `RallyHud` (HUD) et `SoundPlayer` (sons) s'y abonnent.
- Couleurs et matériaux partagés : `Scripts/Art/Palette.cs`. Lumière et brume : `Atmosphere.cs`. Décor : `Court/DecorBuilder.cs`. Personnages : `Player/CharacterModel.cs`. Effets de frappe : `Game/HitEffects.cs`. HUD : `UI/HudSkin.cs` et `DebugUI/RallyHud.cs`.
- Sons : fabriqués par le code au lancement (`Audio/SoundSynth.cs`), aucun fichier audio.
- Police du HUD : celle d'Unity par défaut. Si des fichiers `Baloo2-Medium`, `Baloo2-SemiBold` et `Baloo2-ExtraBold` sont posés dans `Assets/TennisPhone/Resources/Fonts/`, ils sont utilisés.
- Aucun personnage, décor, logo ou interface d'un jeu existant : les personnages et le stade sont des dessins originaux.

### Touches (scène du court)

Clic gauche : coup droit. Clic droit : revers. Flèche haut : lancer la balle de service (ou lift si maintenue pendant un clic). Flèche bas maintenue : slice. `R` : recentrer la raquette. `D` : changer le niveau de l'IA. `T` : mode démonstration (le jeu joue à la place du joueur). `Entrée` : rejouer un set.

### Connexion du téléphone

- Unity sert la page manette en HTTPS sur le port 8443 et la page d'installation du certificat en HTTP sur le port 8081.
- Le pare-feu Windows doit autoriser l'éditeur Unity sur le type de réseau utilisé.
- Le certificat serveur est régénéré tout seul quand l'adresse IP du PC change ; l'autorité installée sur l'iPhone reste valable.

### Outils

- 60 tests EditMode dans `Assets/TennisPhone/Tests/EditMode` (détecteur, physique, frappe, IA, score).
- Menu `TennisPhone > Simuler les frappes de l'adversaire` : taux de fautes de l'IA par niveau.
- Menu `TennisPhone > Capture d'ecran du jeu` : image de la fenêtre Game dans `Temp/Captures`.
- `Recordings/` (non versionné) contient les enregistrements de capteurs ; `SwingReplay` les rejoue dans le détecteur.

### Reste à faire

- Régler l'équilibre en jouant avec l'iPhone : les échanges sont longs (personne ne marque de point gagnant direct) et le niveau normal est probablement trop facile.
- Vérifier les revers liftés et slicés : aucune mesure réelle, le réglage est le miroir du coup droit.
- Écran d'accueil.
- Écouter les sons en jeu et régler leurs volumes (`SoundPlayer`) : ils ont été vérifiés par le code, pas à l'oreille.
- Accorder la page manette (`controller.html`) à la palette (tâche 13 de la charte).
