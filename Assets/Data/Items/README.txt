LEAD KHONG PHANH - ITEM DATA IMPORTER

1. Copy the C# files into the Unity project:

Assets/Scripts/Data/Items/
    GameItemEnums.cs
    ItemStatModifier.cs
    ItemStats.cs
    GameItemData.cs
    GameItemDatabase.cs

Assets/Editor/GAMEPLAY/
    ShopItemExcelImporter.cs

2. Let Unity compile.

3. Use:
LEAD KHONG PHANH
    -> Data
        -> Import 80 Shop Items From XLSX

4. Select:
Danh_Sach_Vat_Pham_Shop_Kem_Chi_So_Buff.xlsx

5. The importer creates/updates:
Assets/GameData/Items/Definitions/
    Item_020_*.asset
    ...
    Item_099_*.asset

and:
Assets/GameData/Items/Database/GameItemDatabase.asset

Important:
- Item IDs 20-99 are preserved.
- Source Buff text is preserved.
- Passive description is preserved.
- Category and source classification are imported.
- Stats are parsed into generic ItemStatModifier entries.
- Rarity is NOT guessed because the Excel source does not contain a rarity column.
- Prices are NOT overwritten because the Excel source does not contain prices.
- Existing asset price/purchasable values are preserved when possible.
- This phase does NOT modify MainMenuShopUI, InventoryOwnership, LuckyWheelManager, or Firestore.
