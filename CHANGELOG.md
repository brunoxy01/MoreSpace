# Histórico

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
