# Recursos e limites

O +Espaço 3.0 amplia o utilitário para Windows. A referência funcional foi a [página pública do CleanUai para Windows](https://clean.oprimo.dev/windows/), consultada em 2 de novembro de 2026. O código e a interface do +Espaço são próprios; não há associação com o produto de referência nem promessa de paridade integral.

| Área | O que está implementado no +Espaço |
|---|---|
| Home e disco | Pizza por volume, ocupação por pasta, navegação com índice e revisão de arquivos. |
| Limpeza | Temporários antigos, sem seleção automática. |
| Caches | Categorias explícitas para Chrome/Edge, VS Code, npm e relatórios de falha, se presentes. Apps abertos são bloqueados. |
| Arquivos grandes | Busca local de arquivos a partir de 100 MB, seleção e revisão. A data mostrada é de modificação. |
| Aplicativos | Pacotes Store/Appx; programas tradicionais abrem o gerenciador oficial do Windows. |
| Dados de apps | Lista de pastas em AppData para avaliação manual; não afirma que uma pasta seja órfã. |
| Lixeira | Ocupação por unidade, abertura no Explorer para restauração e esvaziamento com confirmação. |
| Atualizações | Consulta winget, atualização interativa por ID e acesso à tela de atualizações da Store. |
| Duplicados | Tamanho e SHA-256 completo; uma cópia de referência é protegida e revalidada antes de cada remoção. |
| Inicialização | Estados reconhecidos de Run/StartupApproved; alterações revisadas e reversíveis. |
| Revisão | DNS, verificação de disco sem reparo, otimização, opções de indexação e limpeza de componentes via ferramentas oficiais. |
| Discos externos | Revisão de Thumbs.db/.DS_Store em volumes removíveis e solicitação de ejeção. |
| Bandeja/agendamento | Acesso rápido à janela/processos e análise periódica opcional, somente durante a sessão aberta. Nunca limpa automaticamente. |
| Layout | Menu rolável, barras que quebram em linhas e gráfico sobre a tabela em janelas estreitas. |

## O que esta versão não promete

- Não é uma versão para macOS/ARM64 e não reproduz os doze canais de outro produto em todos os detalhes.
- Não detecta restos de apps com certeza, não remove chaves de Registro órfãs e não executa desinstaladores tradicionais automaticamente.
- Não possui atualização automática do próprio +Espaço, limpeza automática em segundo plano ou monitor completo de rede/bateria na bandeja.
- Não limpa manualmente WinSxS, Prefetch, DriverStore, Installer, Windows Update ou bancos de ícones. A manutenção de componentes usa DISM.
- O gráfico soma tamanhos lógicos, podendo contar hard links em mais de uma pasta; no detector de duplicados, identidades de hard links são deduplicadas. Clones ReFS não têm tratamento específico.
- A Lixeira depende do Windows. Arquivos grandes demais ou certos discos podem exigir exclusão definitiva, confirmada pelo Shell. A API usada para reciclar recusa caminhos de 260 caracteres ou mais; a análise e a exclusão permanente suportam caminhos longos.
- Miniaturas e arquivos AppleDouble `._*` não são limpos indiscriminadamente.

## Proteção e contabilidade

O padrão para arquivos pessoais, duplicados e caches revisados é a Lixeira. Exclusão permanente exige selecionar essa opção e confirmar os caminhos. Temporários na tela Limpeza mantêm a confirmação explícita de exclusão permanente.

Enviar algo à Lixeira **não é contabilizado como espaço liberado**. O histórico registra a operação e a variação observada do espaço livre separadamente; outros programas podem alterar esse valor. O total lógico considera exclusões permanentes feitas pelo app. Desinstalações não têm tamanho presumido.

Windows, Program Files, ProgramData, arquivos de sistema/somente leitura, rede e certos locais sensíveis são protegidos na revisão de arquivos. A lista não identifica todo arquivo importante: sempre revise. Remover um arquivo em pasta sincronizada pode sincronizar a remoção com a nuvem.

## Dependências externas

- Windows 10/11 x64 com .NET Framework 4.8.
- Windows PowerShell 5.1 para inventário/removal Appx e solicitações ao Shell.
- App Installer/winget para a tela de atualizações; não é instalado automaticamente.
- Algumas ferramentas do Windows solicitam UAC. O app registra sua abertura, sem alegar que a tarefa externa terminou.

## Validação

Os testes usam árvores temporárias, entradas fictícias e dados demonstrativos. Cobrem cancelamento, escopo, caminhos longos, alteração de arquivos, índice, histórico, seleção, duplicados e ciclo do menu. A interface é exercitada em 820×650, 1024×768 e 1360×860. Isso não equivale a teste em todas as escalas de DPI, hardware ou versões do Windows.

Não foram desinstalados apps reais, esvaziada a Lixeira do usuário, executados DISM/atualizações nem ejetadas unidades durante a validação. Essas operações dependem do Windows e do consentimento dentro do aplicativo.
