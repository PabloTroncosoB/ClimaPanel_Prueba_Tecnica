# Publicación final en GitHub

Durante la evaluación trabaje localmente. Publique solo después del cierre
común informado por la comisión.

```bash
git init
git add .
git commit -m "chore: código inicial de la evaluación"
git branch -M main
git switch -c solution
```

Realice commits durante el desarrollo. Al finalizar:

```bash
dotnet build --configuration Release
dotnet test --configuration Release
git add .
git commit -m "feat: solución final de la prueba"
git switch main
git merge --no-ff solution -m "merge: solución final"
git tag submission-final
```

Cree un repositorio público vacío y publíquelo:

```bash
git remote add origin https://github.com/USUARIO/climapanel-evaluacion.git
git push -u origin main solution --tags
```

Envíe: URL, SHA del commit etiquetado y URL del tag `submission-final`.
