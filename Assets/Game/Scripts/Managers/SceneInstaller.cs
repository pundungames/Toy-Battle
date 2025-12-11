// ============================================================================
// SCENE INSTALLER - Zenject Dependency Injection
// Tüm manager'ları burada bind ediyoruz
// ❌ REMOVED: SkillSystem
// ============================================================================

using UnityEngine;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Core Managers
        Container.Bind<GameManager>().FromComponentInHierarchy().AsSingle().NonLazy();
        Container.Bind<UIManager>().FromComponentInHierarchy().AsSingle().NonLazy();
        Container.Bind<CurrencyManager>().FromComponentInHierarchy().AsSingle().NonLazy();

        // Game Systems
        Container.Bind<DraftCardManager>().FromComponentInHierarchy().AsSingle().NonLazy();
        Container.Bind<BattleManager>().FromComponentInHierarchy().AsSingle().NonLazy();
        Container.Bind<GridManager>().FromComponentInHierarchy().AsSingle().NonLazy();

        // Bonus System (Bonus cards stay!)
        Container.Bind<BonusSystem>().FromComponentInHierarchy().AsSingle().NonLazy();

        // ❌ REMOVED: SkillSystem
        // Container.Bind<SkillSystem>().FromComponentInHierarchy().AsSingle().NonLazy();

        // Meta Systems
        Container.Bind<ChestSystem>().FromComponentInHierarchy().AsSingle().NonLazy();
        Container.Bind<MergeSystem>().FromComponentInHierarchy().AsSingle().NonLazy();
        Container.Bind<UnlockSystem>().FromComponentInHierarchy().AsSingle().NonLazy();

        // AI & Tutorial
        Container.Bind<AIController>().FromComponentInHierarchy().AsSingle().NonLazy();
        Container.Bind<AITurnManager>().FromComponentInHierarchy().AsSingle().NonLazy();
        Container.Bind<TutorialController>().FromComponentInHierarchy().AsSingle().NonLazy();
    }
}