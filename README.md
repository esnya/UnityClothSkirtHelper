# UnityClothSkirtHelper オープンソース版
UnityのClothコンポーネントを使ってスカートを作る作業を簡略化するエディタ拡張。VRChat向け。

![Screenshot](Documents~/ss01.png)

※ 画像は開発中のものです。

## インストール
Unityを開き、同梱の`ClothSkirtHelper-{VERSION}.unitypackage`をProjectウィンドウのAssetsディレクトリ以下にドラッグ&ドロップする。
最新オープンソース版は[GitHub](https://github.com/esnya/UnityClothSkirtHelper/releases)からダウンロードできます。

## 使い方
メニューの「EsnyaTools」の中にある「Cloth Skirt Helper」からメインウィンドウを開けます。
ClothコンポーネントはCloth化したいSkinned Mesh Rendererをもつオブジェクトに手動で追加してください。
メインウィンドウはClothコンポーネントの右クリックメニューからも開けます。

オブジェクトのTransformはReset状態にしておくことを推奨します。

## おまけ
### Skirt Mesh Tool
Skinned Mesh RendererのMeshをClothスカート用に加工します。

#### Mesh Extractor
必要な部分を取り出します。

#### Inside Deleter
内側を削除します。

#### Mesh Combiner
ふたつのMeshを結合します。

#### Mesh Spreading Deformer
スカートを広げた形に変形します。

### Mesh Cleaner
Skinned Mesh RendererのMeshから使われていない頂点を削除します。

## 更新履歴
[CHANGELOG.md](CHANGELOG.md)

## バグ報告など
オープンソース版は [GitHub Issues](issues) でのみIssueとPull Requestを受け付けます。
その他サポートは有りません。


## ライセンス
Mit License
