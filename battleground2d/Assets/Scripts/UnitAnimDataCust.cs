using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public AnimMaterialTypeEnum ecsAnimTypeEnum;

    public static List<UnitAnimDataCust> unitAnimTypeList;
    public static Dictionary<AnimMaterialTypeEnum, UnitAnimDataCust> unitAnimTypeDic;

    public static List<Material> RunRightMaterials = Resources.LoadAll("Material/RunRight", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunLeftMaterials = Resources.LoadAll("Material/RunLeft", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunUpMaterials = Resources.LoadAll("Material/RunUp", typeof(Material)).Cast<Material>().ToList();
    public static List<Material> RunDownMaterials = Resources.LoadAll("Material/RunDown", typeof(Material)).Cast<Material>().ToList();






    public static void Init()
    {
        unitAnimTypeDic = new Dictionary<AnimMaterialTypeEnum, UnitAnimDataCust>();
        unitAnimTypeList = new List<UnitAnimDataCust>();

        foreach (AnimMaterialTypeEnum animTypeEnum in System.Enum.GetValues(typeof(AnimMaterialTypeEnum)))
        {
            UnitAnimDataCust unitAnimDataCust = GetAnimTypeData(animTypeEnum);
            unitAnimTypeDic[animTypeEnum] = unitAnimDataCust;
            unitAnimTypeList.Add(unitAnimDataCust);
        }
    }

    public static UnitAnimDataCust GetAnimTypeData(AnimMaterialTypeEnum animType)
    {
        switch (animType)
        {
            default:
            //case AnimMaterialTypeEnum.IdleRight:
            //    return new UnitAnimDataCust()
            //    {
            //        CurrentFrame = 0,
            //        FrameCount = 2,
            //        FrameRate = .1f,
            //        VerticalCount = 0,
            //        HorizontalCount = 0
            //    };
            //    break;
            //case AnimMaterialTypeEnum.IdleLeft:
            //    return new UnitAnimDataCust()
            //    {
            //        CurrentFrame = 0,
            //        FrameCount = 2,
            //        FrameRate = .1f,
            //        VerticalCount = 0,
            //        HorizontalCount = 2
            //    };
            //    break;
            //case AnimMaterialTypeEnum.IdleUp:
            //    return new UnitAnimDataCust()
            //    {
            //        CurrentFrame = 0,
            //        FrameCount = 2,
            //        FrameRate = .1f,
            //        VerticalCount = 0,
            //        HorizontalCount = 4
            //    };
            //    break;
            //case AnimMaterialTypeEnum.IdleDown:
            //    return new UnitAnimDataCust()
            //    {
            //        CurrentFrame = 0,
            //        FrameCount = 2,
            //        FrameRate = .1f,
            //        VerticalCount = 0,
            //        HorizontalCount = 6
            //    };
            //    break;


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
                //var test =  Resources.LoadAll("Material/RunRight", typeof(Material)).Cast<Material>().ToList();
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .1f,
                    VerticalCount = 8,
                    HorizontalCount = 0,
                    Materials = RunRightMaterials
                };
                break;
            case AnimMaterialTypeEnum.RunLeft:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .1f,
                    VerticalCount = 8,
                    HorizontalCount = 6,
                    Materials = RunLeftMaterials
                };
                break;
            case AnimMaterialTypeEnum.RunUp:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .1f,
                    VerticalCount = 7,
                    HorizontalCount = 0,
                    Materials = RunUpMaterials
                };
                break;
            case AnimMaterialTypeEnum.RunDown:
                return new UnitAnimDataCust()
                {
                    CurrentFrame = 0,
                    FrameCount = 6,
                    FrameRate = .1f,
                    VerticalCount = 7,
                    HorizontalCount = 6,
                    Materials = RunDownMaterials
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
                //case AnimMaterialTypeEnum.DieRight:
                //    break;
                //case AnimMaterialTypeEnum.DieLeft:
                //    break;
                //case AnimMaterialTypeEnum.DieUp:
                //    break;
                //case AnimMaterialTypeEnum.DieDown:
                //    break;

        }
    }

    public static AnimMaterialTypeEnum GetAnimTypeEnum(int animDir, BaseAnimMaterialType baseAnimTypeEnum)
    {
        switch (baseAnimTypeEnum)
        {
            default:
            //case BaseAnimMaterialType.Idle:
            //    switch (animDir)
            //    {
            //        default:
            //        case 1:
            //            return AnimMaterialTypeEnum.IdleRight;
            //        case 2:
            //            return AnimMaterialTypeEnum.IdleLeft;
            //        case 3:
            //            return AnimMaterialTypeEnum.IdleUp;
            //        case 4:
            //            return AnimMaterialTypeEnum.IdleDown;
            //    }
            //    break;
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
                //case BaseAnimMaterialType.Die:
                //    switch (animDir)
                //    {
                //        default:
                //        case 1:
                //            return AnimMaterialTypeEnum.DieRight;
                //        case 2:
                //            return AnimMaterialTypeEnum.DieLeft;
                //        case 3:
                //            return AnimMaterialTypeEnum.DieUp;
                //        case 4:
                //            return AnimMaterialTypeEnum.DieDown;
                //    }
                //    break;
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
        Die
    }

    public enum AnimMaterialTypeEnum
    {
        //IdleRight,
        //IdleLeft,
        //IdleUp,
        //IdleDown,
        //WalkRight,
        //WalkLeft,
        //WalkUp,
        //WalkDown,
        RunRight,
        RunLeft,
        RunUp,
        RunDown//,
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
        //DieRight,
        //DieLeft,
        //DieUp,
        //DieDown
    }

    public static int GetAnimDir(Vector3 dir)
    {
        float horizontal = dir.x;
        float vertical = dir.z;
        float maxValue = Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.z));

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
    

}


// class will assign sprite sheet data to unit
public class UnitAnimationCust
{
    //return animation data
    public static SpriteSheetAnimationDataCust PlayAnimForced(/*ref UnitParsCust unit,*/ UnitAnimDataCust.BaseAnimMaterialType baseAnimEnum, int animDir, AnimationOnCompleteCust onComplete)
    {
        //unit.spriteSheetData =
            
            return GetSpriteSheetData(baseAnimEnum, animDir, onComplete);
    }

    private static SpriteSheetAnimationDataCust GetSpriteSheetData(UnitAnimDataCust.BaseAnimMaterialType baseAnimEnum, int animDir, AnimationOnCompleteCust onComplete)
    {
        UnitAnimDataCust.AnimMaterialTypeEnum animType = UnitAnimDataCust.GetAnimTypeEnum(animDir, baseAnimEnum);
        UnitAnimDataCust data = UnitAnimDataCust.GetAnimTypeData(animType);
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
            materials = data.Materials
        };
    }


    public static SpriteSheetAnimationDataCust? PlayAnim(/*ref UnitParsCust unit,*/  UnitAnimDataCust.BaseAnimMaterialType baseAnimEnum, SpriteSheetAnimationDataCust spriteSheetAnimationData, int animDir, AnimationOnCompleteCust onComplete)
    {
        if (IsAnimDifferentFromActive(spriteSheetAnimationData, animDir, baseAnimEnum))
        {
            return PlayAnimForced(baseAnimEnum, animDir, onComplete);
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