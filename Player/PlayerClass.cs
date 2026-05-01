using UnityEngine;
using Mirror;
using System;

public class PlayerClass : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnClassChanged))]
    private CharacterClass currentClass = CharacterClass.Swordsman;

    [SyncVar]
    private bool isFirstClass = true;

    public CharacterClass CurrentClass => currentClass;
    public bool IsFirstClass => isFirstClass;

    public event Action<CharacterClass> OnClassChangedEvent;

    [Server]
    public void SetClass(CharacterClass newClass)
    {
        currentClass = newClass;
    }

    [Server]
    public bool PromoteClass(CharacterClass promotedClass)
    {
        if (!CanPromote(promotedClass)) return false;

        currentClass = promotedClass;
        isFirstClass = false;

        if (TryGetComponent(out PlayerAnimation anim))
        {
            CharacterClassData classData = Resources.Load<CharacterClassData>($"Classes/{promotedClass}");
            if (classData != null)
            {
                RuntimeAnimatorController controller = GetComponent<PlayerStats>()?.IsMale == true
                    ? classData.maleAnimator : classData.femaleAnimator;

                if (controller != null)
                    anim.SetAnimatorController(controller);
            }
        }

        return true;
    }

    private bool CanPromote(CharacterClass promotedClass)
    {
        if (TryGetComponent(out PlayerStats stats))
        {
            if (stats.Level < 40) return false;
        }

        return promotedClass switch
        {
            CharacterClass.Champion => currentClass == CharacterClass.Swordsman,
            CharacterClass.Crusader => currentClass == CharacterClass.Swordsman,
            CharacterClass.Sharpshooter => currentClass == CharacterClass.Hunter,
            CharacterClass.Voyager => currentClass == CharacterClass.Explorer,
            CharacterClass.Cleric => currentClass == CharacterClass.Herbalist,
            _ => false
        };
    }

    private void OnClassChanged(CharacterClass oldClass, CharacterClass newClass)
    {
        OnClassChangedEvent?.Invoke(newClass);
    }
}