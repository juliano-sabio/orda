# Projeto Horda / Orda — instruções para o Claude Code

Jogo de horda roguelike em Unity 2D.

## Git LFS (importante)

As cenas do Unity (`*.unity`) são rastreadas via **Git LFS** — veja `.gitattributes`.
Elas são arquivos grandes (dezenas de MB) porque o Unity reserializa o YAML
inteiro a cada save.

**Antes de trabalhar neste repo, rode uma vez por máquina:**

```bash
git lfs install
```

Sem isso, ao clonar/puxar você recebe apenas os **ponteiros** das cenas (um
textinho com `version https://git-lfs.github.com/...`) em vez do conteúdo real,
e o Unity não abre as cenas.

- Use `git add/commit/push/pull` normalmente — o LFS é transparente.
- Não converta as cenas de volta para Git normal nem remova as linhas `*.unity`
  do `.gitattributes`.
- A migração foi *forward-only*: o histórico antigo não foi reescrito.
