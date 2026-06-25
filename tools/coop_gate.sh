#!/usr/bin/env bash
# Gate de paridade co-op (filtro). Aponta o "cheiro" de código centrado no player 1:
# finders globais de player que deveriam usar PlayerStats.Local / MaisProximo / roteamento-pro-dono.
#
# Uso:  bash tools/coop_gate.sh
# Saída: lista suja (arquivo:linha:trecho). Sai com código 1 se houver violação (pra usar como gate).
#
# Escape hatch: uma linha com o marcador  coop-local-ok  é ignorada (efeito/UI local de propósito).
#
# NÃO pega (precisa de revisão pela árvore de decisão do doc, não por grep):
#   - UI compartilhada construída só no host (P5)
#   - Instantiate de visual fora do NetSpawn (P1)

set -u
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC="$ROOT/horda/Assets/scripts"

# Padrões proibidos (finders globais de player).
PAT='FindGameObjectWithTag\("Player"\)|FindGameObjectsWithTag\("Player"\)|FindFirstObjectByType<PlayerStats>|FindAnyObjectByType<PlayerStats>|FindObjectOfType<PlayerStats>|FindObjectsOfType<PlayerStats>'

echo "== Gate de paridade co-op =="
echo "Fonte: $SRC"
echo

# grep recursivo em .cs, remove o que tem o escape hatch.
HITS="$(grep -rnE --include='*.cs' "$PAT" "$SRC" 2>/dev/null | grep -v 'coop-local-ok')"

if [ -z "$HITS" ]; then
  echo "LIMPO: nenhum finder global de player. 🎉"
  exit 0
fi

echo "$HITS"
echo
N="$(printf '%s\n' "$HITS" | grep -c .)"
echo "----------------------------------------"
echo "VIOLAÇÕES: $N (cada uma → árvore de decisão do doc → porta certa, ou marque coop-local-ok)"
exit 1
