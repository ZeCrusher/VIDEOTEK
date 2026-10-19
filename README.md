# VIDEOTEK
Convertisseur de vidéo via FFMPEG (Et pourquoi pas !!)


Capture d'écran du soft :

<img width="1232" height="814" alt="Capture d&#39;écran 2026-10-19 152518" src="https://github.com/user-attachments/assets/011466db-5847-4b0b-af74-fb2427652791" />


Comment compiler ça :

Forcer le nettoyage préalable (dotnet clean)
La méthode la plus propre consiste à supprimer les anciens fichiers compilés (bin et obj) juste avant de publier. 

**C:\utils\C\**dotnet clean FFmpegConverter.csproj -c Release ; dotnet publish FFmpegConverter.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

Sinon, vous pouvez faire aussi la compil avec un dossier de sortie propre (-o ou --output)
Forcer la publication vers un dossier spécifique : 
dotnet publish FFmpegConverter.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish

Pour que tout fonctionne vous devez avoir ça :

📁 VIDEOTEK /

├── 📄 VIDEOTEK.exe   

└── 📄 ffmpeg.exe        (L'exécutable FFmpeg téléchargé)

