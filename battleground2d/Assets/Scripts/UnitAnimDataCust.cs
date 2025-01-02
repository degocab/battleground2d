using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class UnitAnimDataCust
{

    public int CurrentFrame;
    public int FrameCount;
    public float FrameTimerMax;
    public int VerticalCount;
    public int HorizontalCount;

    public float FrameRate;

    public List<Material> Materials;
    public List<Material> MaterialsEnemy;

    public AnimMaterialTypeEnum ecsAnimTypeEnum;

    public static List<UnitAnimDataCust> unitAnimTypeList;
    public static Dictionary<AnimMaterialTypeEnum, UnitAnimDataCust> unitAnimTypeDic;

    #region DefaulUnitMaterials
    public static List<Material> RunRightMaterialsDefault = Resources.LoadAll("Material/Default/RunRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunLeftMaterialsDefault = Resources.LoadAll("Material/Default/RunLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunUpMaterialsDefault = Resources.LoadAll("Material/Default/RunUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunDownMaterialsDefault = Resources.LoadAll("Material/Default/RunDown", typeof(Material)).Cast<Material>().ToList();

    public static List<Material> IdleRightMaterialsDefault = Resources.LoadAll("Material/Default/IdleRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleLeftMaterialsDefault = Resources.LoadAll("Material/Default/IdleLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleUpMaterialsDefault = Resources.LoadAll("Material/Default/IdleUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleDownMaterialsDefault = Resources.LoadAll("Material/Default/IdleDown", typeof(Material)).Cast<Material>().ToList();


    public static List<Material> AttackRightMaterialsDefault = Resources.LoadAll("Material/Default/AttackRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> AttackLeftMaterialsDefault = Resources.LoadAll("Material/Default/AttackLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> AttackUpMaterialsDefault = Resources.LoadAll("Material/Default/AttackUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> AttackDownMaterialsDefault = Resources.LoadAll("Material/Default/AttackDown", typeof(Material)).Cast<Material>().ToList();

    public static List<Material> DieRightMaterialsDefault = Resources.LoadAll("Material/Default/DieRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieLeftMaterialsDefault = Resources.LoadAll("Material/Default/DieLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieUpMaterialsDefault = Resources.LoadAll("Material/Default/DieUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieDownMaterialsDefault = Resources.LoadAll("Material/Default/DieDown", typeof(Material)).Cast<Material>().ToList();





    public static List<Material> RunRightMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/RunRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunLeftMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/RunLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunUpMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/RunUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunDownMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/RunDown", typeof(Material)).Cast<Material>().ToList();

    public static List<Material> IdleRightMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/IdleRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleLeftMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/IdleLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleUpMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/IdleUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleDownMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/IdleDown", typeof(Material)).Cast<Material>().ToList();


    public static List<Material> ShootRightMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/ShootRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> ShootLeftMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/ShootLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> ShootUpMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/ShootUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> ShootDownMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/ShootDown", typeof(Material)).Cast<Material>().ToList();


    public static List<Material> Shoot45RightMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/Shoot45Right", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> Shoot45LeftMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/Shoot45Left", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> Shoot45UpMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/Shoot45Up", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> Shoot45DownMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/Shoot45Down", typeof(Material)).Cast<Material>().ToList();

    public static List<Material> DieRightMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/DieRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieLeftMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/DieLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieUpMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/DieUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieDownMaterialsDefaultArcher = Resources.LoadAll("Material/Default/Archer/DieDown", typeof(Material)).Cast<Material>().ToList();






    #endregion

    #region EnemyUnitMaterials

    public static List<Material> RunRightMaterialsEnemy = Resources.LoadAll("Material/Enemy/RunRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunLeftMaterialsEnemy = Resources.LoadAll("Material/Enemy/RunLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunUpMaterialsEnemy = Resources.LoadAll("Material/Enemy/RunUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunDownMaterialsEnemy = Resources.LoadAll("Material/Enemy/RunDown", typeof(Material)).Cast<Material>().ToList();

    public static List<Material> IdleRightMaterialsEnemy = Resources.LoadAll("Material/Enemy/IdleRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleLeftMaterialsEnemy = Resources.LoadAll("Material/Enemy/IdleLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleUpMaterialsEnemy = Resources.LoadAll("Material/Enemy/IdleUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleDownMaterialsEnemy = Resources.LoadAll("Material/Enemy/IdleDown", typeof(Material)).Cast<Material>().ToList();


    public static List<Material> AttackRightMaterialsEnemy = Resources.LoadAll("Material/Enemy/AttackRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> AttackLeftMaterialsEnemy = Resources.LoadAll("Material/Enemy/AttackLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> AttackUpMaterialsEnemy = Resources.LoadAll("Material/Enemy/AttackUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> AttackDownMaterialsEnemy = Resources.LoadAll("Material/Enemy/AttackDown", typeof(Material)).Cast<Material>().ToList();

    public static List<Material> DieRightMaterialsEnemy = Resources.LoadAll("Material/Enemy/DieRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieLeftMaterialsEnemy = Resources.LoadAll("Material/Enemy/DieLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieUpMaterialsEnemy = Resources.LoadAll("Material/Enemy/DieUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieDownMaterialsEnemy = Resources.LoadAll("Material/Enemy/DieDown", typeof(Material)).Cast<Material>().ToList();





    public static List<Material> RunRightMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/RunRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunLeftMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/RunLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunUpMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/RunUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunDownMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/RunDown", typeof(Material)).Cast<Material>().ToList();

    public static List<Material> IdleRightMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/IdleRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleLeftMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/IdleLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleUpMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/IdleUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> IdleDownMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/IdleDown", typeof(Material)).Cast<Material>().ToList();


    public static List<Material> ShootRightMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/ShootRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> ShootLeftMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/ShootLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> ShootUpMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/ShootUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> ShootDownMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/ShootDown", typeof(Material)).Cast<Material>().ToList();


    public static List<Material> Shoot45RightMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/Shoot45Right", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> Shoot45LeftMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/Shoot45Left", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> Shoot45UpMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/Shoot45Up", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> Shoot45DownMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/Shoot45Down", typeof(Material)).Cast<Material>().ToList();

    public static List<Material> DieRightMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/DieRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieLeftMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/DieLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieUpMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/DieUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> DieDownMaterialsEnemyArcher = Resources.LoadAll("Material/Enemy/Archer/DieDown", typeof(Material)).Cast<Material>().ToList();

    #endregion
    public static void Init()
    {
        // I DONT THINK THIS IS USED AT ALL????
        //unitAnimTypeDic = new Dictionary<AnimMaterialTypeEnum, UnitAnimDataCust>();
        //unitAnimTypeList = new List<UnitAnimDataCust>();

        //foreach (AnimMaterialTypeEnum animTypeEnum in System.Enum.GetValues(typeof(AnimMaterialTypeEnum)))
        //{
        //    UnitAnimDataCust unitAnimDataCust = GetAnimTypeData(animTypeEnum);
        //    unitAnimTypeDic[animTypeEnum] = unitAnimDataCust;
        //    unitAnimTypeList.Add(unitAnimDataCust);
        //}
    }

    public static UnitAnimDataCust GetAnimTypeData(AnimMaterialTypeEnum animType, string unitType, bool isEnemy)
    {
        switch (animType)
        {
            default:
            case AnimMaterialTypeEnum.IdleRight:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 2,
                    FrameRate = .1f,
                    VerticalCount = 0,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.IdleRight, unitType, isEnemy),//IdleRightMaterials,
                    MaterialsEnemy = IdleRightMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.IdleLeft:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 2,
                    FrameRate = .1f,
                    VerticalCount = 0,
                    HorizontalCount = 2,
                    Materials = GetMaterials(AnimMaterialTypeEnum.IdleLeft, unitType, isEnemy),//IdleLeftMaterials,
                    MaterialsEnemy = IdleLeftMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.IdleUp:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 2,
                    FrameRate = .1f,
                    VerticalCount = 0,
                    HorizontalCount = 4,
                    Materials = GetMaterials(AnimMaterialTypeEnum.IdleUp, unitType, isEnemy),//IdleUpMaterials,
                    MaterialsEnemy = IdleUpMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.IdleDown:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 2,
                    FrameRate = .1f,
                    VerticalCount = 0,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.IdleDown, unitType, isEnemy),//IdleDownMaterials,
                    MaterialsEnemy = IdleDownMaterialsEnemy
                };
                break;


            //case AnimMaterialTypeEnum.WalkRight:
            //    return new UnitAnimDataCust()
            //    {
            //        CurrentFrame = 0,
            //        FrameCount = 4,
            //        FrameRate = .1475f,
            //        VerticalCount = 6,
            //        HorizontalCount = 0
            //    };
            //    break;
            //case AnimMaterialTypeEnum.WalkLeft:
            //    return new UnitAnimDataCust()
            //    {
            //        CurrentFrame = 0,
            //        FrameCount = 4,
            //        FrameRate = .1475f,
            //        VerticalCount = 6,
            //        HorizontalCount = 4
            //    };
            //    break;
            //case AnimMaterialTypeEnum.WalkUp:
            //    return new UnitAnimDataCust()
            //    {
            //        CurrentFrame = 0,
            //        FrameCount = 4,
            //        FrameRate = .1475f,
            //        VerticalCount = 6,
            //        HorizontalCount = 8
            //    };
            //    break;
            //case AnimMaterialTypeEnum.WalkDown:
            //    return new UnitAnimDataCust()
            //    {
            //        CurrentFrame = 0,
            //        FrameCount = 4,
            //        FrameRate = .1475f,
            //        VerticalCount = 5,
            //        HorizontalCount = 0
            //    };
            //    break;



            case AnimMaterialTypeEnum.RunRight:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .14f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    //Materials = RunRightMaterials,
                    Materials = GetMaterials(AnimMaterialTypeEnum.RunRight, unitType, isEnemy),
                    MaterialsEnemy = RunRightMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.RunLeft:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .14f,
                    VerticalCount = 8,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.RunLeft, unitType, isEnemy),//RunLeftMaterials,
                    MaterialsEnemy = RunLeftMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.RunUp:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .14f,
                    VerticalCount = 7,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.RunUp, unitType, isEnemy),//RunUpMaterials,
                    MaterialsEnemy = RunUpMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.RunDown:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .14f,
                    VerticalCount = 7,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.RunDown, unitType, isEnemy),//RunDownMaterials,
                    MaterialsEnemy = RunDownMaterialsEnemy
                };
                break;



            //case AnimMaterialTypeEnum.DamagedRight:
            //    break;
            //case AnimMaterialTypeEnum.DamagedLeft:
            //    break;
            //case AnimMaterialTypeEnum.DamagedUp:
            //    break;
            //case AnimMaterialTypeEnum.DamagedDown:
            //    break;
            //case AnimMaterialTypeEnum.BlockRight:
            //    break;
            //case AnimMaterialTypeEnum.BlockLeft:
            //    break;
            //case AnimMaterialTypeEnum.BlockUp:
            //    break;
            //case AnimMaterialTypeEnum.BlockDown:
            //    break;
            //case AnimMaterialTypeEnum.DefendRight:
            //    break;
            //case AnimMaterialTypeEnum.DefendLeft:
            //    break;
            //case AnimMaterialTypeEnum.DefendUp:
            //    break;
            //case AnimMaterialTypeEnum.DefendDown:
            //    break;
            case AnimMaterialTypeEnum.DieRight:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .1f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.DieRight, unitType, isEnemy),//DieRightMaterials,
                    MaterialsEnemy = DieRightMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.DieLeft:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .1f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.DieLeft, unitType, isEnemy),//DieLeftMaterials,
                    MaterialsEnemy = DieLeftMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.DieUp:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .1f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.DieUp, unitType, isEnemy),//DieUpMaterials,
                    MaterialsEnemy = DieUpMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.DieDown:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .1f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.DieDown, unitType, isEnemy),//DieDownMaterials,
                    MaterialsEnemy = DieDownMaterialsEnemy
                };
                break;



            case AnimMaterialTypeEnum.AttackRight:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .15f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.AttackRight, unitType, isEnemy),//AttackRightMaterials,
                    MaterialsEnemy = AttackRightMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.AttackLeft:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .15f,
                    VerticalCount = 8,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.AttackLeft, unitType, isEnemy),//AttackLeftMaterials,
                    MaterialsEnemy = AttackLeftMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.AttackUp:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .15f,
                    VerticalCount = 7,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.AttackUp, unitType, isEnemy),//AttackUpMaterials,
                    MaterialsEnemy = AttackUpMaterialsEnemy
                };
                break;
            case AnimMaterialTypeEnum.AttackDown:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .15f,
                    VerticalCount = 7,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.AttackDown, unitType, isEnemy),//AttackDownMaterials,
                    MaterialsEnemy = AttackDownMaterialsEnemy
                };
                break;



            case AnimMaterialTypeEnum.ShootRight:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 5,
                    FrameRate = .75f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.ShootRight, unitType, isEnemy),///*ShootRightMaterials*/,
                    MaterialsEnemy = ShootRightMaterialsEnemyArcher
                };
                break;
            case AnimMaterialTypeEnum.ShootLeft:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 5,
                    FrameRate = .75f,
                    VerticalCount = 8,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.ShootLeft, unitType, isEnemy),//ShootLeftMaterials,
                    MaterialsEnemy = ShootLeftMaterialsEnemyArcher
                };
                break;
            case AnimMaterialTypeEnum.ShootUp:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 5,
                    FrameRate = .75f,
                    VerticalCount = 7,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.ShootUp, unitType, isEnemy),//ShootUpMaterials,
                    MaterialsEnemy = ShootUpMaterialsEnemyArcher
                };
                break;
            case AnimMaterialTypeEnum.ShootDown:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 5,
                    FrameRate = .75f,
                    VerticalCount = 7,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.ShootDown, unitType, isEnemy),//ShootDownMaterials,
                    MaterialsEnemy = ShootDownMaterialsEnemyArcher
                };
                break;





            case AnimMaterialTypeEnum.Shoot45Right:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 5,
                    FrameRate = .1f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.Shoot45Right, unitType, isEnemy),///*Shoot45RightMaterials*/,
                    MaterialsEnemy = Shoot45RightMaterialsEnemyArcher
                };
                break;
            case AnimMaterialTypeEnum.Shoot45Left:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 5,
                    FrameRate = .1f,
                    VerticalCount = 8,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.Shoot45Left, unitType, isEnemy),//Shoot45LeftMaterials,
                    MaterialsEnemy = Shoot45LeftMaterialsEnemyArcher
                };
                break;
            case AnimMaterialTypeEnum.Shoot45Up:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 5,
                    FrameRate = .1f,
                    VerticalCount = 7,
                    HorizontalCount = 0,
                    Materials = GetMaterials(AnimMaterialTypeEnum.Shoot45Up, unitType, isEnemy),//Shoot45UpMaterials,
                    MaterialsEnemy = Shoot45UpMaterialsEnemyArcher
                };
                break;
            case AnimMaterialTypeEnum.Shoot45Down:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 5,
                    FrameRate = .1f,
                    VerticalCount = 7,
                    HorizontalCount = 6,
                    Materials = GetMaterials(AnimMaterialTypeEnum.Shoot45Down, unitType, isEnemy),//Shoot45DownMaterials,
                    MaterialsEnemy = Shoot45DownMaterialsEnemyArcher
                };
                break;
        }
    }

    private static List<Material> GetMaterials(AnimMaterialTypeEnum typeEnum, string unitType, bool isEnemy)
    {
        if (isEnemy)
        {

        }
        return GetPropValue(typeEnum.ToString() + "Materials" + (isEnemy ? "Enemy" : "Default") + unitType);
    }

    public static List<Material> GetPropValue( string propName)
    {
        var propertyInfo = typeof(UnitAnimDataCust).GetField(propName);
        return (List<Material>)propertyInfo.GetValue(null);
    }


    public static AnimMaterialTypeEnum GetAnimTypeEnum(int animDir, BaseAnimMaterialType baseAnimTypeEnum)
    {
        switch (baseAnimTypeEnum)
        {
            default:
            case BaseAnimMaterialType.Idle:
                switch (animDir)
                {
                    default:
                    case 1:
                        return AnimMaterialTypeEnum.IdleRight;
                    case 2:
                        return AnimMaterialTypeEnum.IdleLeft;
                    case 3:
                        return AnimMaterialTypeEnum.IdleUp;
                    case 4:
                        return AnimMaterialTypeEnum.IdleDown;
                }
                break;
            //case BaseAnimMaterialType.Walk:
            //    switch (animDir)
            //    {
            //        default:
            //        case 1:
            //            return AnimMaterialTypeEnum.WalkRight;
            //        case 2:
            //            return AnimMaterialTypeEnum.WalkLeft;
            //        case 3:
            //            return AnimMaterialTypeEnum.WalkUp;
            //        case 4:
            //            return AnimMaterialTypeEnum.WalkDown;
            //    }
            //    break;
            case BaseAnimMaterialType.Run:
                switch (animDir)
                {
                    default:
                    case 1:
                        return AnimMaterialTypeEnum.RunRight;
                    case 2:
                        return AnimMaterialTypeEnum.RunLeft;
                    case 3:
                        return AnimMaterialTypeEnum.RunUp;
                    case 4:
                        return AnimMaterialTypeEnum.RunDown;
                }
                break;
            //case BaseAnimMaterialType.Damgaged:
            //    switch (animDir)
            //    {
            //        default:
            //        case 1:
            //            return AnimMaterialTypeEnum.DamagedRight;
            //        case 2:
            //            return AnimMaterialTypeEnum.DamagedLeft;
            //        case 3:
            //            return AnimMaterialTypeEnum.DamagedUp;
            //        case 4:
            //            return AnimMaterialTypeEnum.DamagedDown;
            //    }
            //    break;
            //case BaseAnimMaterialType.Block:
            //    switch (animDir)
            //    {
            //        default:
            //        case 1:
            //            return AnimMaterialTypeEnum.BlockRight;
            //        case 2:
            //            return AnimMaterialTypeEnum.BlockLeft;
            //        case 3:
            //            return AnimMaterialTypeEnum.BlockUp;
            //        case 4:
            //            return AnimMaterialTypeEnum.BlockDown;
            //    }
            //    break;
            //case BaseAnimMaterialType.Defend:
            //    switch (animDir)
            //    {
            //        default:
            //        case 1:
            //            return AnimMaterialTypeEnum.DefendRight;
            //        case 2:
            //            return AnimMaterialTypeEnum.DefendLeft;
            //        case 3:
            //            return AnimMaterialTypeEnum.DefendUp;
            //        case 4:
            //            return AnimMaterialTypeEnum.DefendDown;
            //    }
            //    break;
            case BaseAnimMaterialType.Die:
                switch (animDir)
                {
                    default:
                    case 1:
                        return AnimMaterialTypeEnum.DieRight;
                    case 2:
                        return AnimMaterialTypeEnum.DieLeft;
                    case 3:
                        return AnimMaterialTypeEnum.DieUp;
                    case 4:
                        return AnimMaterialTypeEnum.DieDown;
                }
                break;
            case BaseAnimMaterialType.Attack:
                switch (animDir)
                {
                    default:
                    case 1:
                        return AnimMaterialTypeEnum.AttackRight;
                    case 2:
                        return AnimMaterialTypeEnum.AttackLeft;
                    case 3:
                        return AnimMaterialTypeEnum.AttackUp;
                    case 4:
                        return AnimMaterialTypeEnum.AttackDown;
                }
                break;
            case BaseAnimMaterialType.Shoot:
                switch (animDir)
                {
                    default:
                    case 1:
                        return AnimMaterialTypeEnum.ShootRight;
                    case 2:
                        return AnimMaterialTypeEnum.ShootLeft;
                    case 3:
                        return AnimMaterialTypeEnum.ShootUp;
                    case 4:
                        return AnimMaterialTypeEnum.ShootDown;
                }
                break;
            case BaseAnimMaterialType.Shoot45:
                switch (animDir)
                {
                    default:
                    case 1:
                        return AnimMaterialTypeEnum.Shoot45Right;
                    case 2:
                        return AnimMaterialTypeEnum.Shoot45Left;
                    case 3:
                        return AnimMaterialTypeEnum.Shoot45Up;
                    case 4:
                        return AnimMaterialTypeEnum.Shoot45Down;
                }
                break;
        }
    }


    public enum BaseAnimMaterialType
    {
        Idle,
        Walk,
        Run,
        Damgaged,
        Block,
        Defend,
        Die,
        Attack,

        Shoot,
        Shoot45
    }

    public enum AnimMaterialTypeEnum
    {
        IdleRight,
        IdleLeft,
        IdleUp,
        IdleDown,
        //WalkRight,
        //WalkLeft,
        //WalkUp,
        //WalkDown,
        RunRight,
        RunLeft,
        RunUp,
        RunDown,
        //DamagedRight,
        //DamagedLeft,
        //DamagedUp,
        //DamagedDown,
        //BlockRight,
        //BlockLeft,
        //BlockUp,
        //BlockDown,
        //DefendRight,
        //DefendLeft,
        //DefendUp,
        //DefendDown,
        DieRight,
        DieLeft,
        DieUp,
        DieDown,
        AttackRight,
        AttackLeft,
        AttackUp,
        AttackDown,
        ShootRight,
        ShootLeft,
        ShootUp,
        ShootDown,
        Shoot45Right,
        Shoot45Left,
        Shoot45Up,
        Shoot45Down
    }

    public static int GetAnimDir(Vector3 dir)
    {
        float horizontal = dir.x;
        float vertical = dir.y;
        float maxValue = Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));

        int direction = 1;
        if (vertical > 0 && Mathf.Abs(vertical) == maxValue) // up
        {
            direction = 3;
        }
        else if (vertical < 0 && Mathf.Abs(vertical) == maxValue) // down
        {
            direction = 4;
        }
        else if (horizontal > 0 && Mathf.Abs(horizontal) == maxValue) // right
        {
            direction = 1;
        }
        else if (horizontal < 0 && Mathf.Abs(horizontal) == maxValue) // left
        {
            direction = 2;
        }

        return direction;
    }



    private static UnitAnimDataCust instance = null;
    private static readonly object padlock = new object();

    UnitAnimDataCust()
    {

    }
    public static UnitAnimDataCust Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new UnitAnimDataCust();
                }
                return instance;
            }
        }
    }

}







public struct PlayAnimationCust
{
    public int animDir;
    public bool forced;
    public AnimationOnCompleteCust onComplete;
    public UnitAnimDataCust.BaseAnimMaterialType baseAnimType;

    public void PlayAnim(UnitAnimDataCust.BaseAnimMaterialType baseAnimType, Vector3 dir, AnimationOnCompleteCust onComplete)
    {
        PlayAnim(baseAnimType, UnitAnimDataCust.GetAnimDir(dir), false, onComplete);
    }

    public void PlayAnim(UnitAnimDataCust.BaseAnimMaterialType baseAnimType, int dir, bool forced, AnimationOnCompleteCust onComplete)
    {
        this.animDir = dir;
        this.forced = forced;
        this.onComplete = onComplete;
        this.baseAnimType = baseAnimType;
    }

}

public struct AnimationOnCompleteCust
{
    public bool hasOnComplete;
    public int animDir;
    public UnitAnimDataCust.BaseAnimMaterialType baseAnimTypeEnum;

    public static AnimationOnCompleteCust Create(UnitAnimDataCust.BaseAnimMaterialType baseAnimTypeEnum, Vector3 dir)
    {
        return Create(baseAnimTypeEnum, UnitAnimDataCust.GetAnimDir(dir));
    }

    private static AnimationOnCompleteCust Create(UnitAnimDataCust.BaseAnimMaterialType baseAnimTypeEnum, int dir)
    {
        return new AnimationOnCompleteCust
        {
            hasOnComplete = true,
            baseAnimTypeEnum = baseAnimTypeEnum,
            animDir = dir
        };
    }
}


//this stores all sprite sheet data to run animations off of
//this is the data used in RenderAnimation()
public struct SpriteSheetAnimationDataCust
{
    public int currentFrame;
    public int frameCount;
    public float frameTimer;
    public float frameTimerMax;

    public int activeDir;
    public UnitAnimDataCust.BaseAnimMaterialType activeBaseAnimTypeEnum;

    public AnimationOnCompleteCust onComplete;
    public float frameRate;
    public int loopCount;

    public List<Material> materials;

    public List<Material> materialsEnemy;
}


// class will assign sprite sheet data to unit
public class UnitAnimationCust
{
    //return animation data
    public static SpriteSheetAnimationDataCust PlayAnimForced(/*ref UnitParsCust unit,*/ UnitAnimDataCust.BaseAnimMaterialType baseAnimEnum, int animDir, AnimationOnCompleteCust onComplete
                                                              ,string unitType, bool isEnemy)
    {
        //unit.spriteSheetData =

        return GetSpriteSheetData(baseAnimEnum, animDir, onComplete, unitType, isEnemy);
    }

    private static SpriteSheetAnimationDataCust GetSpriteSheetData(UnitAnimDataCust.BaseAnimMaterialType baseAnimEnum, int animDir, AnimationOnCompleteCust onComplete
                                                                   , string unitType, bool isEnemy )
    {
        UnitAnimDataCust.AnimMaterialTypeEnum animType = UnitAnimDataCust.GetAnimTypeEnum(animDir, baseAnimEnum);
        UnitAnimDataCust data = UnitAnimDataCust.GetAnimTypeData(animType, unitType, isEnemy);
        return new SpriteSheetAnimationDataCust
        {
            frameCount = data.FrameCount,
            currentFrame = 0,
            loopCount = 0,
            frameTimer = 0f,
            frameRate = data.FrameRate,
            activeDir = animDir,
            activeBaseAnimTypeEnum = baseAnimEnum,
            onComplete = onComplete,
            materials = data.Materials,
            materialsEnemy = data.MaterialsEnemy
        };
    }


    public static SpriteSheetAnimationDataCust? PlayAnim(/*ref UnitParsCust unit,*/  UnitAnimDataCust.BaseAnimMaterialType baseAnimEnum, SpriteSheetAnimationDataCust spriteSheetAnimationData, int animDir, AnimationOnCompleteCust onComplete
                                                        , string unitType, bool isEnemy)
    {
        if (IsAnimDifferentFromActive(spriteSheetAnimationData, animDir, baseAnimEnum))
        {
            return PlayAnimForced(baseAnimEnum, animDir, onComplete, unitType, isEnemy);
        }
        return (SpriteSheetAnimationDataCust?)null;
    }

    private static bool IsAnimDifferentFromActive(SpriteSheetAnimationDataCust spriteSheetAnimationData, int animDir, UnitAnimDataCust.BaseAnimMaterialType baseAnimEnum)
    {
        if (baseAnimEnum == spriteSheetAnimationData.activeBaseAnimTypeEnum && animDir == spriteSheetAnimationData.activeDir)
        {
            return false;
        }
        else
        {
            if (baseAnimEnum != spriteSheetAnimationData.activeBaseAnimTypeEnum)
            {
                //differnt anim, same dir
                return true;
            }
            else if (baseAnimEnum == spriteSheetAnimationData.activeBaseAnimTypeEnum && animDir != spriteSheetAnimationData.activeDir)
            {
                //same base anim, diff dir
                return true;
            }
            else
            {
                //different dir, same animation
                return true;
            }
        }
    }
}