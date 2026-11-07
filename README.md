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

`Game` é a única cena habilitada nas configurações de build. O arraste por
eventos de ponteiro também permite testar a interação no Editor.

### Celular (Android e iOS)

As configurações de loja e de player ficam no asset
`Assets/Editor/OndeVai/Mobile/MobileBuildProfile.asset` (nome, bundle id,
versão, ícone, orientação, SDKs, arquiteturas, keystore). Elas são aplicadas
automaticamente antes de toda build Android/iOS, ou manualmente em
**Tools > Onde Vai > Mobile > Apply Mobile Player Settings**.

- **APK de teste:** *Tools > Onde Vai > Mobile > Build Android APK (Development)*.
- **App Bundle para a Play Store:** *Build Android App Bundle (Release)*. Requer
  `KeystorePath` e `KeyAliasName` no perfil e as variáveis de ambiente
  `ANDROID_KEYSTORE_PASS` e `ANDROID_KEYALIAS_PASS`.
- **iOS:** *Export iOS Xcode Project* (precisa do módulo iOS instalado); a
  assinatura e o envio são feitos no Xcode, em um Mac.
- **Linha de comando / CI:** `-executeMethod OndeVai.EditorTools.Mobile.MobileBuild.BuildAndroidRelease`.
  O workflow `.github/workflows/build-mobile.yml` gera a build Android em tags `v*`
  ou manualmente.

O jogo roda apenas em retrato. A câmera se ajusta à proporção da tela
(`CameraFitter`) para que as caixas não sejam cortadas em celulares altos.

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

Todo o conteúdo é definido por `ScriptableObject`s em [`Assets/Data`](Assets/Data).
Existe um único prefab de item ([`Assets/Prefabs/Items/Item.prefab`](Assets/Prefabs/Items/Item.prefab));
o sprite, o colisor, a escala e a física são aplicados a partir dos dados quando
o item surge.

| Asset | Conteúdo |
|---|---|
| `GameDefinition` | Prefab de item, lista de tipos de item e lista de cenários. |
| `ItemTypes/*.asset` (`ItemTypeDefinition`) | Grupo (`ItemGroup`), nome em português, ícone, cor, escala, forma do colisor, física e a lista de **variantes** (sprite, id, peso de sorteio, escala, cor, colisor, ativo). |
| `Scenarios/*.asset` (`SortingScenarioDefinition`) | Id, nome em português e os dois tipos de item a separar. |

Para adicionar itens, abra o tipo (por exemplo `Toys`) e arraste sprites ou
folhas de sprites para a área indicada no Inspector. Cada sprite vira uma
variante. Um cenário só aparece nas opções quando os dois tipos têm ao menos uma
variante ativa com sprite. Atalhos em **Tools > Onde Vai > Content**: abrir o
`GameDefinition`, criar tipo, criar cenário e validar o conteúdo.

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
