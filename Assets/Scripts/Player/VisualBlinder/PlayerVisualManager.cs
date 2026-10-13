using UnityEngine;

/// <summary>
/// Central coordinator cho toàn bộ Player visual binders.
///
/// Execution:
/// PlayerVisualManager (-200)
///        ↓
/// Vehicle Binder (-100)
///        ↓
/// Outfit / Cosmetic / Accessory Binder
///
/// Manager chỉ chịu trách nhiệm:
/// - Resolve database
/// - Configure binder
/// - Refresh
/// - Clear
///
/// Không chạy per-frame.
/// </summary>
[DefaultExecutionOrder(-200)]
public sealed class PlayerVisualManager : MonoBehaviour
{
    //=====================================================================
    // DATABASE
    //=====================================================================

    [Header("Database")]
    [SerializeField]
    private GameItemDatabase itemDatabase;

    [SerializeField]
    private GameItemVisualDatabase visualDatabase;


    //=====================================================================
    // BINDERS
    //=====================================================================

    [Header("Visual Binders")]
    [SerializeField]
    private PlayerVehicleVisualBinder vehicleBinder;

    [SerializeField]
    private PlayerOutfitVisualBinder outfitBinder;

    [SerializeField]
    private PlayerCosmeticVisualBinder cosmeticBinder;

    [SerializeField]
    private PlayerAccessoryVisualBinder accessoryBinder;


    //=====================================================================
    // STATE
    //=====================================================================

    private bool initialized;


    //=====================================================================
    // UNITY
    //=====================================================================

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
    }


    //=====================================================================
    // INITIALIZATION
    //=====================================================================

    public void Initialize()
    {
        if (initialized)
            return;

        ResolveBinderReferences();

        if (itemDatabase == null)
        {
            Debug.LogError(
                "[PlayerVisualManager] GameItemDatabase chưa được assign.",
                this);

            return;
        }

        if (visualDatabase == null)
        {
            Debug.LogError(
                "[PlayerVisualManager] GameItemVisualDatabase chưa được assign.",
                this);

            return;
        }

        itemDatabase.Initialize();
        visualDatabase.Initialize();

        ConfigureBinders();

        initialized = true;
    }


    //=====================================================================
    // RESOLVE BINDERS
    //=====================================================================

    private void ResolveBinderReferences()
    {
        if (vehicleBinder == null)
        {
            vehicleBinder =
                GetComponentInChildren<PlayerVehicleVisualBinder>(
                    true);
        }

        if (outfitBinder == null)
        {
            outfitBinder =
                GetComponentInChildren<PlayerOutfitVisualBinder>(
                    true);
        }

        if (cosmeticBinder == null)
        {
            cosmeticBinder =
                GetComponentInChildren<PlayerCosmeticVisualBinder>(
                    true);
        }

        if (accessoryBinder == null)
        {
            accessoryBinder =
                GetComponentInChildren<PlayerAccessoryVisualBinder>(
                    true);
        }
    }


    //=====================================================================
    // CONFIGURE
    //=====================================================================

    private void ConfigureBinders()
    {
        if (vehicleBinder != null)
        {
            vehicleBinder.Configure(
                itemDatabase,
                visualDatabase);
        }

        if (outfitBinder != null)
        {
            outfitBinder.Configure(
                itemDatabase,
                visualDatabase);
        }

        if (cosmeticBinder != null)
        {
            cosmeticBinder.Configure(
                itemDatabase,
                visualDatabase);
        }

        if (accessoryBinder != null)
        {
            accessoryBinder.Configure(
                itemDatabase,
                visualDatabase);
        }
    }


    //=====================================================================
    // REFRESH
    //=====================================================================

    public void RefreshAll()
    {
        if (!initialized)
        {
            Initialize();

            if (!initialized)
                return;
        }

        if (vehicleBinder != null)
        {
            vehicleBinder.ApplyEquippedItem();
        }

        if (outfitBinder != null)
        {
            outfitBinder.ApplyEquippedItem();
        }

        if (cosmeticBinder != null)
        {
            cosmeticBinder.ApplyEquippedItem();
        }

        if (accessoryBinder != null)
        {
            accessoryBinder.ApplyEquippedItem();
        }
    }


    //=====================================================================
    // REFRESH CATEGORY
    //=====================================================================

    public void RefreshCategory(
        GameItemCategory category)
    {
        if (!initialized)
        {
            Initialize();

            if (!initialized)
                return;
        }

        switch (category)
        {
            case GameItemCategory.Vehicle:

                if (vehicleBinder != null)
                {
                    vehicleBinder.ApplyEquippedItem();
                }

                break;


            case GameItemCategory.Outfit:

                if (outfitBinder != null)
                {
                    outfitBinder.ApplyEquippedItem();
                }

                break;


            case GameItemCategory.Cosmetic:

                if (cosmeticBinder != null)
                {
                    cosmeticBinder.ApplyEquippedItem();
                }

                break;


            case GameItemCategory.Accessory:

                if (accessoryBinder != null)
                {
                    accessoryBinder.ApplyEquippedItem();
                }

                break;
        }
    }


    //=====================================================================
    // CLEAR
    //=====================================================================

    public void ClearAll()
    {
        if (vehicleBinder != null)
        {
            vehicleBinder.ClearVisual();
        }

        if (outfitBinder != null)
        {
            outfitBinder.ClearVisual();
        }

        if (cosmeticBinder != null)
        {
            cosmeticBinder.ClearVisual();
        }

        if (accessoryBinder != null)
        {
            accessoryBinder.ClearVisual();
        }
    }


    //=====================================================================
    // GET BINDER
    //=====================================================================

    public bool TryGetBinder(
        GameItemCategory category,
        out PlayerCategoryVisualBinderBase binder)
    {
        binder = null;

        switch (category)
        {
            case GameItemCategory.Vehicle:

                binder = vehicleBinder;
                break;


            case GameItemCategory.Outfit:

                binder = outfitBinder;
                break;


            case GameItemCategory.Cosmetic:

                binder = cosmeticBinder;
                break;


            case GameItemCategory.Accessory:

                binder = accessoryBinder;
                break;
        }

        return binder != null;
    }


    //=====================================================================
    // DATABASE ACCESS
    //=====================================================================

    public GameItemDatabase ItemDatabase
    {
        get { return itemDatabase; }
    }

    public GameItemVisualDatabase VisualDatabase
    {
        get { return visualDatabase; }
    }


    //=====================================================================
    // BINDER ACCESS
    //=====================================================================

    public PlayerVehicleVisualBinder VehicleBinder
    {
        get { return vehicleBinder; }
    }

    public PlayerOutfitVisualBinder OutfitBinder
    {
        get { return outfitBinder; }
    }

    public PlayerCosmeticVisualBinder CosmeticBinder
    {
        get { return cosmeticBinder; }
    }

    public PlayerAccessoryVisualBinder AccessoryBinder
    {
        get { return accessoryBinder; }
    }
}