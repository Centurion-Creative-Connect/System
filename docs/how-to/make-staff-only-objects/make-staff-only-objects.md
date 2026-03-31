# スタッフ/プレイヤーにのみ見えるオブジェクトを設定する

[StaffOnlyObjectToggleCommand](/commands/staffonlyobjecttogglecommand) を利用することで、
スタッフにのみ、もしくはプレイヤーにのみ見えるオブジェクトを設定できます。

## 設定方法

1. Command として登録するため、任意の NewbieConsoleCommandRegisterer がついている GameObject を用意します。
    1. サンプルシーンで試す場合は `Logics/System/SceneDependentCommands` にて既に設定されているため、これを利用すると良いでしょう。
2. 1 で用意した GameObject の子として、Empty な GameObject を新規作成します。
    1. このとき、新規作成した GameObject の名前を、利用したいコマンド名へと置き換えてください。
3. 2 で新規作成した GameObject にコンポーネント [StaffOnlyObjectToggleCommand](/commands/staffonlyobjecttogglecommand)
   を追加します。
4. [StaffOnlyObjectToggleCommand](/commands/staffonlyobjecttogglecommand) の表を参考に、実現したい挙動に沿ったリストへ、対象の
   GameObject を設定します。

## Tips

- 権限を持つユーザーは `<コマンド名> [true|false]` の形で、指定されたオブジェクトをオンオフできます。
    - Default State を切り替えることができるイメージ
    - 自動的に同期されます
- Centurion では、`Moderator Only Objects To Always Enable` を用いてスタッフに対してコントロールパネル等を表示し、
  `Player Only Objects To Enable` を用いてプレイヤー向け侵入不可コライダーなどを制御しています。
