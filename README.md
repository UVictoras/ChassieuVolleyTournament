# Tournoi de Chassieu Volley

Application Windows (WinForms, .NET Framework 4.7.2) de gestion du tournoi : affichage public, fenêtre staff, et page web pour les arbitres.

## Lancer
Ouvrir `ChassieuVolleyTournament.sln` dans Visual Studio puis F5, ou lancer `ChassieuVolleyTournament.exe` (les images sont copiées à côté de l'exe).

## Page arbitres (n'importe quel réseau)
Au démarrage l'application crée automatiquement un lien Internet `https://xxxx.trycloudflare.com` (Cloudflare Quick Tunnel : sans compte, sans réglage de box, sans droits administrateur).
Le lien est affiché dans la fenêtre staff (bouton **Copier**). Il change à chaque lancement.
`cloudflared.exe` est téléchargé une seule fois dans `%LOCALAPPDATA%\ChassieuVolleyTournament` (ou placez-le à côté de l'exe / dans le PATH).
Le PC du tournoi doit avoir Internet ; les téléphones peuvent être sur 4G ou n'importe quel Wi-Fi.
Si l'application est lancée en administrateur, un lien réseau local (même Wi-Fi) est aussi proposé.

L'arbitre ouvre le lien, saisit la clef du match (affichée dans la fenêtre staff), puis touche son équipe pour ajouter un point.
Un match terminé est automatiquement verrouillé : les arbitres ne peuvent plus le modifier. Le bouton **Verrouillage des matchs** (fenêtre staff) permet de le rouvrir pour une correction, puis de le reverrouiller (en phase finale, les matchs ne se verrouillent que par ce bouton).

## Noms des équipes
Créez `teams.txt` (16 lignes) à côté de l'exe pour remplacer les noms par défaut.

## Journal
`%LOCALAPPDATA%\ChassieuVolleyTournament\log.txt`
