# TennisPhone

Jeu de tennis Unity, solo contre une IA, vue derrière le joueur. L'iPhone sert de raquette : il ouvre une page web servie par Unity et envoie ses capteurs par WebSocket.

## Cadrage validé (2026-11-16)

- **Moteur** : Unity 6000.6.9f1. Pilotage de l'éditeur par MCP for Unity (CoplayDev).
- **Référence de gameplay** : MinimumTennis (MIT, lecture du code C#) + tetetennis (sans licence : relever des valeurs uniquement, via REA sur la démo web) + règles officielles du tennis. Résultat dans `REFERENCE.md`, chaque élément étiqueté « observé », « déduit » ou « inconnu » avec sa source. Aucun code ni asset repris.
- **Téléphone** : iPhone (Safari). Connexion directe en Wi-Fi en HTTPS/WSS avec un certificat local (profil à installer et approuver une fois sur l'iPhone). Pas de tunnel.
- **Page manette** : bouton « Activer la raquette » (permission DeviceMotion iOS déclenchée par un clic), envoi DeviceMotion + DeviceOrientation à 60 Hz.
- **Vibration** : Safari iOS n'a pas d'API de vibration. Tenter un contournement, avec son + flash d'écran en repli.
- **Joueur** : déplacement automatique, le joueur ne gère que le swing. Droitier par défaut, réglable.
- **Swing** : pic d'accélération + vitesse angulaire. Timing → direction (tôt = croisé, tard = long de ligne), intensité → puissance, rotation du poignet → lift ou slice.
- **Service** : deux gestes (un geste vers le haut lance la balle, un swing la frappe).
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
