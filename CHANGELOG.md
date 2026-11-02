# 3.1.0

- Temas Sistema, Claro e Escuro com preferência persistente e atualização automática conforme o Windows.
- Link suport us para o repositório no rodapé da navegação.
- Bordas animadas nos módulos e botões, respeitando animações do Windows.
- Contraste de listas, revisões e controles adaptado ao tema; testes de troca de tema e layout.
- Novos prints demonstrativos no README.

# Histórico

## 3.0.0

- Layout adaptável a partir de 820×650, menu rolável, barras de ações com quebra de linha e Home empilhada em janelas estreitas.
- Duplicados por tamanho/SHA-256, identidade de arquivo e preservação de uma cópia revalidada.
- Lixeira como padrão para arquivos revisados, consulta por unidade e abertura para restauração.
- Caches conhecidos com bloqueio de aplicativos em execução; revisão manual de dados em AppData.
- Atualizações via winget/Store e ferramentas oficiais de revisão do Windows.
- Metadados em discos removíveis, solicitação de ejeção, bandeja e análise periódica opcional durante a sessão.
- Galeria de sete capturas demonstrativas, README renovado, matriz de recursos/limites, guia de contribuição e formulários de issues.
- Testes de novos recursos e de redimensionamento; atualização das actions de CI.

## 2.4.1

- Corrige `ObjectDisposedException` em `ContextMenuStrip` ao usar o menu da pizza/lista: o menu permanece válido durante a vida da janela e não é descartado no evento Closed.
- Teste de regressão com 30 ciclos de abertura/fechamento, troca de controle de origem, acionamento de opções e descarte ao fechar a janela.

## 2.4.0

- Arquivos grandes inicia a busca automaticamente nos discos locais ao abrir a tela, com resultados progressivos e ordenação final por tamanho.
- Seleção e exclusão revisada de arquivos grandes, integrada ao histórico.
- Cartão de espaço liberado com cantos arredondados e animações de hover/clique.
- Suporte a caminhos longos no runtime e manifesto; teste com caminhos acima de 260 caracteres.

## 2.3.0

- Menu de contexto na pizza e na lista de ocupação para revisar e excluir arquivos da pasta.
- Seleção individual, revisão de caminhos e confirmação de exclusão permanente, sem remoção de subpastas.
- Revalidação de arquivos antes da exclusão, proteção de locais do sistema e registro por item.
- Arquivos excluídos pela Home incluídos no histórico de espaço liberado.
- Testes isolados de escopo, alterações de arquivo e atributos protegidos.

## 2.2.0

- Enumeração única de diretórios com metadados em cache, evitando consultas redundantes por arquivo.
- Índice de tamanhos de todas as subpastas: navegação sem releitura após a primeira análise.
- Horário do índice visível, atualização explícita e invalidação após operações de limpeza/remoção.
- Indicador de espaço liberado e histórico persistente com tamanho lógico separado da variação observada no espaço livre.
- Testes de desempenho, cache, cancelamento e persistência do histórico.

## 2.1.0

- Home com gráfico de pizza das partições e exploração de pastas/subpastas por clique, com retorno ao nível superior e cancelamento.
- Nova marca visual: HD com símbolo +, em PNG transparente e ICO de múltiplas resoluções.
- Removido o subtítulo “CUIDE DO SEU WINDOWS” abaixo da marca.
- Adicionadas caixas para selecionar vários aplicativos, selecionar somente os visíveis e limpar a seleção.
- Seleção preservada entre filtros; indicação de itens selecionados fora do filtro.
- Confirmação com lista completa, remoção sequencial, parada após o aplicativo atual e resultado por item.
- Botões arredondados com transições curtas de hover e clique; suporte a foco e teclado preservado.
- Listas com buffer duplo e leitura de processos fora da thread da interface.
- Logo própria e ícone Windows em múltiplas resoluções embutido no executável.
- Publicação inicial do código, build reproduzível e testes automatizados.

## 2.0.0

- Marca +Espaço e navegação lateral.
- Processos com CPU, RAM e pesquisa.
- Controle de inicialização e remoção individual de aplicativos Store/Appx.

## 1.0.0

- Scanner de temporários, arquivos grandes e consumo de memória.
