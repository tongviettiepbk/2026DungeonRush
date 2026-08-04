using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TypeMenuLobby
{
    None = 0,
    Main,
    Shop,
    Pet,
    Mode,
    Clan
}

public class UIMainLobby : BaseUI
{
    public Toggle togleAutoPet;
    public List<ElementPetUILobby> listElementPet;

    [Space(20)]
    public List<ElementEquipmentUILobby> listElementEquipment;

    [Space(20)]
    public List<ElementTabMenuUILobby> listElementMenu;

    [Space(20)]
    public Button btAutoLoot;
    public Button btLoot;
    public Button btBoost;

    private void Start()
    {

    }

    private void ClickTooglePet()
    {

    }



}
