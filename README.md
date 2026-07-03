lilEmo
====

自分用に作成した非破壊・コンポーネント完結型の表情設定ツールです。片手ずつの表情制御とメニューでの表情制御のみを行います。表情が多すぎても覚えられないため、右手左手の特定の組み合わせでの表情操作は実装していません。

## 使い方

アバター配下にGameObjectを作ってEmoコンポーネントを追加して表情を設定するだけです。

1. アバター配下にGameObjectを作成
2. 作成したGameObjectにEmoコンポーネントを追加
3. ジェスチャー、手、表情を設定

### テンプレート機能

アバターを選択しメニューバーの`Tools/lilEmo/Template (Any Hand)`または`Template (Left and Right Hand)`を実行すると全てのジェスチャー分のGameObjectを一括追加できます。Any Handは両手に同じ表情を割り当てるテンプレート、Left and Right Handは右手と左手に別々の表情を割り当てる際のテンプレートです。

### まばたき（VRChat）

まばたき干渉対策のため、VRChatのまばたき設定は自動でAnimationClipでのまばたきに置き換えられます。まばたきに使われるBlendShapeはAvatarDescriptorから取得されるため、AvatarDescriptorのまばたき設定自体はしておいてください。

### AnimationClip

コンポーネントで表情指定する代わりにAnimationClipを指定することもできます。マルチフレーム対応です。

### カスタム条件

カスタム条件はContactを使って撫でたときに表情が変わるギミックなど他パラメーターを利用したい場合に使います（条件はAND）。VRChatで使う場合、パラメーターはExpressionParametersに自動追加されないので、MA Parametersなどで適宜追加してください。

### メニュー整理（VRChat）

通常はメニュールート直下にExpressionsフォルダが生成され、その中に表情メニューが追加されます。ただし、Emoコンポーネントが付いたGameObjectの親にMA Menu Installerが存在する場合はビルド時にGameObjectにMA Menu Itemが自動追加されます。つまりMA Menu Item同様にメニュー整理できます。Idleの表情のメニューの場所も変更したい場合はEmo Idle Placeholderを追加したGameObjectをEmo同様にMA Menu Installer配下に配置してください。

### 表情の優先度

メニュー、カスタム条件、右手、左手の順に優先されます。

## その他コンポーネント

### Emo Idle Placeholder

Modular Avatarを使ってメニューを手動で整理する場合にIdleの表情のメニュー位置を指定する際に使用します。詳しくは上記の「メニュー整理」を参照してください。

### Emo Additional Transition

レアケースですが、カスタム条件をORで追加したい場合はEmoのGameObjectにこのコンポーネントを追加して設定します。

### Emo Transform

例えばけもみみを動かすなど、表情と連動してTransformを動かしたい場合はEmoのGameObjectにこのコンポーネントを追加して設定します。

### Emo Settings

遷移時間やアイコン解像度、アイコンキャプチャの際のカメラなど生成内容を手動調整する場合に使用します。他コンポーネントと異なりアバターに1つだけつけるコンポーネントです。このコンポーネントと同じGameObjectにMA Menu Installerが付いている場合はツールによる自動生成せずにそちらを使用します（つまりMenu Installerの設定を自由に変更できます）。

## 内部動作（VRChat）

### AnimatorController

以下のパラメーターが追加されます。

- lilEmo: メニュー操作用int
- lilEmoDisableBlink: まばたき無効化用bool
- カスタム条件で指定したパラメーター

また以下2つのレイヤーが追加されます。

- lilEmo: 表情全て
- lilEmoBlink: まばたき（Stateはまばたきオン、まばたきオフのAnimationClipを再生する2つのみ）

### AnimationClip

1フレームのループなしAnimationClipが生成されます。AnimationClip指定時は0フレーム目に他コンポーネントで操作されていてかつこのAnimationClipに登録されていないBlendShapeが自動追加されます。

### State

Write Defaultsオンです。ただし、オンでもオフでも動作するように組まれているためWrite Defaults由来の不具合を回避できるようになっています。またStateそれぞれに以下のBehaviourが追加されます。

- VRC Avatar Parameter Driver: まばたき無効化用（lilEmoDisableBlinkをtrueまたはfalseに設定）
- VRC Animator Tracking Control: Eyes & Eyelidsはアイトラ無効化、Mouth & Jawはリップシンク無効化でそれぞれ切り替え、他はNo Change

### 生成プロセス

- 表情用アイコンをキャプチャ
- MA Merge Animatorを絶対パスモードで生成
- MA Parametersを生成し、メニュー操作用にint型の同期パラメーター`lilEmo`を1つ追加
- コンポーネントをAnimatorControllerに変換＆MA Menu Itemを生成、表情アイコンを割り当て
- MA Menu InstallerにないEmoがあった場合は親にMA Menu InstallerとSubMenuのMA Menu Itemを生成
- 生成したAnimatorControllerをMA Merge Animatorに割り当て
- MA MMD Layer Controlを生成したAnimatorControllerの全Layerに追加
- 不要になったlilEmoのコンポーネントを削除

## 内部動作（Emock）

単にEmockのコンポーネントに自動変換します。
