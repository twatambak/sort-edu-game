# Mobile Edu Game

Jogo 2D de classificação de objetos, feito em Unity. O jogador arrasta itens
para caixas correspondentes ao tipo do objeto. O projeto inclui pontuação,
limite de erros, cronômetro, telas de resultado e armazenamento local das
partidas concluídas.

## Sumário

- [Como abrir e executar](#como-abrir-e-executar)
- [Como jogar](#como-jogar)
- [Regras e fluxo da partida](#regras-e-fluxo-da-partida)
- [Configuração e conteúdo](#configuração-e-conteúdo)
- [Implementação](#implementação)
- [Persistência do histórico](#persistência-do-histórico)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Tecnologias e dependências](#tecnologias-e-dependências)
- [Estado atual e observações](#estado-atual-e-observações)

## Como abrir e executar

1. Abra o projeto pelo Unity Hub usando a versão **Unity 6000.3.16f1**.
2. Abra `Assets/Scenes/Game.unity`.
3. Execute a cena pelo botão **Play** do Editor.

`Game` é a única cena habilitada nas configurações de build. A configuração do
projeto permite orientação retrato e paisagem; o arraste por eventos de ponteiro
também permite testar a interação no Editor.

## Como jogar

1. Arraste um brinquedo ou ferramenta até a caixa que representa sua categoria.
2. Um item colocado na caixa correta é removido e soma um ponto.
3. Uma tentativa incorreta conta como erro; o item é repelido pela caixa e
   permanece disponível para outra tentativa.
4. A partida termina quando uma condição de encerramento é atingida. A tela de
   resultado mostra a pontuação e a quantidade de erros.

As categorias declaradas pelo jogo são `Toy`, `Food`, `Tool` e `Animal`. No
conteúdo configurado atualmente, a partida usa brinquedos e ferramentas, cada
qual com sua caixa correspondente.

## Regras e fluxo da partida

### Modo clássico

O modo padrão é definido em `GameConfiguration.ClassicMode`:

| Parâmetro | Valor padrão |
|---|---:|
| Duração da partida | 30 segundos |
| Limite de erros | 3 |
| Itens simultâneos | 5 |
| Pontos para vencer | 99 |
| Atraso entre ondas | 5 segundos |
| Grupos de itens | Brinquedos e ferramentas |

O controlador sorteia o grupo de cada item com chances iguais entre os dois
grupos configurados e, em seguida, sorteia um prefab daquele grupo. Cada item é
criado em um dos pontos de surgimento configurados na cena. Quando todos os
itens da onda forem classificados corretamente, uma nova onda é criada. Os
pontos disponíveis são usados sem repetição antes de serem reutilizados, e as
colisões físicas entre itens são ignoradas para evitar sobreposição instável;
as colisões com as caixas continuam ativas.

### Encerramento

- **Limite de erros:** ao atingir o limite configurado, a partida termina como
  derrota (`ErrorLimitReached`).
- **Meta de pontos:** ao alcançar `ItemsToWin`, termina como vitória
  (`ItemsCollected`).
- **Cronômetro:** ao chegar a zero, a implementação atual termina como vitória
  (`TimeExpired`).

Uma nova partida pelo botão de jogar novamente reinicia os contadores e volta
para o modo clássico.

## Configuração e conteúdo

`GameDefinition` é um `ScriptableObject` que associa cada `ItemGroup` a uma
lista de prefabs. O asset usado pela cena é
[`Assets/Data/GameDefinition.asset`](Assets/Data/GameDefinition.asset). Para
adicionar itens à seleção aleatória:

1. Crie ou ajuste um prefab de item.
2. Defina seu tipo (`ItemGroup`) no componente `Item`.
3. Adicione o prefab à lista do grupo correspondente no asset `GameDefinition`.
4. Configure uma caixa (`Storage`) com o mesmo tipo para aceitar o item.

Os prefabs de brinquedos, ferramentas e caixas ficam em
[`Assets/Prefabs/Items`](Assets/Prefabs/Items). A cena fornece os pontos de
surgimento e referencia o asset `GameDefinition`.

## Implementação

### Controle e eventos

`GameController` é o ponto central da sessão e herda de `SingletonBase`.
Mantém a configuração corrente, pontuação, erros e estado de término. Ele
notifica os componentes de interface por eventos:

- `OnGameStarted`
- `OnCorrectStorage`
- `OnIncorrectStorage`
- `OnGameWon`
- `OnGameLost`

O controlador inicia o modo clássico ao carregar a cena, cria as ondas e
centraliza os resultados de classificação e de cronômetro. `SingletonBase`
mantém uma instância persistente entre cenas e destrói instâncias duplicadas.

### Arraste e classificação

`DragAndDrop` implementa os eventos de ponteiro do Unity. Converte a posição do
ponteiro da tela para coordenadas do mundo, preserva o deslocamento inicial do
arraste e procura alvos sobrepostos ao soltar o item. Se houver um
`Rigidbody2D`, ele fica cinemático durante o arraste e retorna ao tipo anterior
ao soltar.

`Storage` implementa `IDropTarget`. Compara o tipo do item com o tipo da caixa:

- Em caso correto, notifica o controlador, destrói o item e anima a caixa.
- Em caso incorreto, aplica uma força de ejeção quando o item tem
  `Rigidbody2D`, anima a caixa e notifica um erro.

O componente `Item` expõe o grupo do objeto e pode ajustar sua escala conforme
a posição vertical.

### Interface e animações

- `GameTimer` atualiza o slider do tempo e notifica o controlador quando o tempo
  termina.
- `ErrorCounter` cria os indicadores de erro para a sessão e marca cada erro.
- `GameStatusScreen` apresenta vitória/derrota, pontuação e erros, e permite
  iniciar outra partida.
- `GameSetupScreen` contém a lógica para ler parâmetros de configuração.
- `GameVersion` exibe a versão configurada para o aplicativo.
- `Tweenimation` e `Tweenimation3D` reúnem animações reutilizáveis de feedback,
  como `Pop`, `Jelly`, `Spring` e `Impact`.
- `SpriteAnimator` reproduz animações quadro a quadro com sprites configurados
  no Inspector.

## Persistência do histórico

As partidas concluídas são gravadas localmente em SQLite no arquivo
`game-history.db3`, dentro de `Application.persistentDataPath`. Os registros
incluem horários UTC, duração, parâmetros usados, grupos, pontuação, erros,
resultado e motivo do encerramento. Partidas em andamento não são gravadas e os
dados não são sincronizados entre dispositivos.

O histórico pode ser consultado por meio de `GameController.Instance`:

```csharp
var recentes = GameController.Instance.GetRecentGameSessions(20);
int total = GameController.Instance.GetCompletedGameCount();
GameSessionRecord partida = GameController.Instance.GetGameSessionById(1);
```

`GetRecentGameSessions` retorna primeiro os registros mais recentes e exige um
limite positivo. `GetGameSessionById` retorna `null` quando não encontra o ID.
Mais detalhes estão em [`DATABASE.md`](DATABASE.md).

## Estrutura do projeto

| Caminho | Conteúdo |
|---|---|
| `Assets/Scenes/` | Cena principal `Game.unity` |
| `Assets/Scripts/` | Controle de partida, itens, caixas, UI, animações e histórico |
| `Assets/Data/` | Definição de grupos e prefabs disponíveis |
| `Assets/Prefabs/Items/` | Prefabs de itens e caixas de classificação |
| `Assets/Prefabs/` | Prefabs auxiliares da interface |
| `Assets/Sprites/` | Sprites e pacotes de arte do projeto |
| `Assets/Resources/` | Fontes e recursos carregados pela aplicação |
| `Assets/Settings/` | Configurações do Universal Render Pipeline |
| `Packages/manifest.json` | Dependências do Unity |
| `ProjectSettings/` | Versão, cenas de build e configurações do aplicativo |

## Tecnologias e dependências

- **Unity 6000.3.16f1**
- **Universal Render Pipeline** para renderização
- **Input System**, **Unity UI (uGUI)** e **TextMesh Pro** para interação e UI
- **SQLite-net** para o histórico local
- **DOTween** incluído em `Assets/Plugins/Demigiant` para animações
- **NaughtyAttributes** para atributos auxiliares do Inspector

## Estado atual e observações

- A cena inicia diretamente no modo clássico; não apresenta a tela de
  configuração como fluxo inicial.
- `GameSetupScreen` está desativada na cena e seus campos de entrada aparecem
  sem referências atribuídas no Inspector. Embora exista código para ler
  configurações personalizadas, essa tela precisa ser ligada e configurada
  antes de ser usada.
- `DelayBetweenSpawns` é armazenado na configuração e no histórico, mas não é
  aplicado atualmente: a próxima onda é criada assim que todos os itens ativos
  forem classificados.
- O encerramento por tempo é registrado como vitória na implementação atual.
- As categorias `Food` e `Animal` existem no enum, mas não estão incluídas no
  asset de definição usado pela cena.
- Se a inicialização ou gravação do SQLite falhar, o erro é registrado no
  console; a partida pode continuar, mas seu resultado não fica salvo.
