# Okutanga PDF – MVP leitor PDF

Documento de **especificação e decisões** para o MVP do leitor multiplataforma.

## Objectivo do MVP

Permitir que o utilizador **abra um ficheiro PDF** no dispositivo e **leia** com navegação básica, lista de recentes e tema claro/escuro — em Android, iOS, Windows e Mac.

## Fora de âmbito (MVP)

- Sincronização cloud ou conta de utilizador.
- Anotações, assinatura digital, OCR.
- Edição ou criação de PDF.

## Fluxos principais

1. **Arranque** → splash → painel inicial (`/home`).
2. **Abrir PDF** → FilePicker → leitor (`/leitor`) com documento carregado.
3. **Recentes** → biblioteca (`/biblioteca`) → reabrir documento.

## Critérios de aceitação (F1)

1. Botão «Abrir PDF» no painel inicial invoca o selector de ficheiros.
2. Ficheiro `.pdf` seleccionado abre no ecrã de leitor.
3. Utilizador consegue percorrer páginas (scroll ou controlos).
4. Tema claro/escuro funciona no leitor e no resto da app.
5. Build Release compila para pelo menos **Windows** e **Android**.

## Decisões técnicas (a confirmar na implementação)

| Área | Opção MVP | Notas |
|------|-----------|--------|
| Render | WebView + pdf.js ou SkiaSharp | pdf.js reutiliza stack Blazor; SkiaSharp mais nativo |
| Recentes | SQLite | Tabela `RecentDocument` (path, title, opened_at) |
| Permissões | MAUI Essentials FilePicker | Android 13+ scoped storage |

## Referências

- Requisitos: `doc/REQUISITOS.md`
- Projeto: `okutangaPDF.csproj`
