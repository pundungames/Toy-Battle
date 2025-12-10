using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class LevelMergeWindow : EditorWindow
{
    // Editör penceresinde atanacak Transform referansları
    public Transform lv1;
    public Transform lv2;
    public Transform lv3;
    public Transform root; // TurtleNinja olarak adlandırılan ana kök obje

    // --- Pencereyi Açma ---
    [MenuItem("Tools/Level Merge Tool")]
    public static void ShowWindow()
    {
        // Pencereyi oluşturur ve ismini Level Merge Tool olarak ayarlar
        GetWindow<LevelMergeWindow>("Level Merge Tool");
    }

    // --- Editör Arayüzü ---
    private void OnGUI()
    {
        GUILayout.Label("Merge Children By Name (Robust)", EditorStyles.boldLabel);

        // Transform alanlarını oluşturur
        lv1 = (Transform)EditorGUILayout.ObjectField("LV1 (Kaynak)", lv1, typeof(Transform), true);
        lv2 = (Transform)EditorGUILayout.ObjectField("LV2 (Kaynak)", lv2, typeof(Transform), true);
        lv3 = (Transform)EditorGUILayout.ObjectField("LV3 (Kaynak)", lv3, typeof(Transform), true);
        root = (Transform)EditorGUILayout.ObjectField("Root (Hedef/TurtleNinja)", root, typeof(Transform), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Merge Now"))
        {
            MergeProcess();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("LV1/LV2/LV3 altındaki çocukların **isimleriyle** Root altındaki objeler bulunur. Root objesi LV'nin altına taşınır ve orijinal LV çocuğu silinir.", MessageType.Info);
    }

    // --- Birleştirme İşlemini Başlatma ---
    private void MergeProcess()
    {
        if (lv1 == null || lv2 == null || lv3 == null || root == null)
        {
            Debug.LogError("Eksik referans var. LV1, LV2, LV3 ve Root hepsi atanmalı.");
            return;
        }

        // Tüm işlemleri tek bir Undo grubu içine alırız.
        int undoGroup = Undo.GetCurrentGroup();

        int moved = 0;
        int deleted = 0;

        // Her seviye için birleştirme fonksiyonunu çağırırız
        moved += MergeLevel(lv1, ref deleted);
        moved += MergeLevel(lv2, ref deleted);
        moved += MergeLevel(lv3, ref deleted);

        // Tüm sahnelerin değiştiğini işaretler (Kaydetme uyarısı için)
        if (moved > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
        }

        // Undo grubu ismini belirleriz.
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"✅ Merge tamamlandı. Toplam Taşınan: **{moved}**, Toplam Silinen: **{deleted}**");
    }

    // --- Tek Bir Seviye İçin Birleştirme Mantığı ---
    private int MergeLevel(Transform level, ref int deletedCount)
    {
        if (level == null) return 0;

        Debug.Log($"--- {level.name} Seviyesi Başlatıldı ---");

        Transform[] childrenToProcess = new Transform[level.childCount];
        for (int i = 0; i < level.childCount; i++)
            childrenToProcess[i] = level.GetChild(i);

        int moved = 0;

        foreach (Transform oldChild in childrenToProcess)
        {
            if (oldChild == null) continue;

            // 1) Root altında aynı isimli hedefi bul
            Transform target = root.Find(oldChild.name);

            if (target == null)
            {
                Debug.LogWarning($"[Merge] Root objesi ({root.name}) içinde **'{oldChild.name}'** bulunamadı. (Level: {level.name}) Bu obje atlanıyor.");
                continue;
            }

            // Silme işleminden ÖNCE, eski objenin adını kaydediyoruz.
            string oldChildName = oldChild.name;

            // --- Taşıma (Merge) İşlemi ---

            // a) Hedef objeyi (target) level'ın (lv1, lv2, lv3) altına taşır.
            Undo.SetTransformParent(target, level, $"Reparent {target.name} to {level.name}");

            // b) Pozisyon, rotasyon ve ölçek değerlerini de eski objeden alır.
            Undo.RecordObject(target, $"Update Transform of {target.name}");
            target.localPosition = oldChild.localPosition;
            target.localRotation = oldChild.localRotation;
            target.localScale = oldChild.localScale;
            moved++;

            // c) Orijinal (eski) objeyi silme işlemi (Undo destekli)
            try
            {
                // DestroyObjectImmediate editörde silme ve Undo için en iyi yoldur.
                Undo.DestroyObjectImmediate(oldChild.gameObject);
                deletedCount++;

                // Kaydedilen adı kullanıyoruz. Artık oldChild'ın kendisini çağırmıyoruz.
                Debug.Log($"[Merge] ✅ '{target.name}' Root'tan Level'a taşındı. Eski obje '{oldChildName}' silindi. (Level: {level.name})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Merge] ❌ Eski obje '{oldChildName}' silinemedi. Hata: {ex.Message}");
            }
        }

        Debug.Log($"--- {level.name} Seviyesi Tamamlandı. Taşınan: {moved} ---");
        return moved;
    }
}