using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Horror audio v13b: benetako grabazioak eta urrats aldakorrak.
/// Jokoaren mekanikak ez ditu aldatzen; egungo egoerak bakarrik irakurtzen ditu.
/// </summary>
public sealed class HorrorAudioV13 : MonoBehaviour
{
    public AudioClip menuGiroa, bunkerGiroa, basoGiroa, pausoa, tiroa, birkarga, etsaia, kolpea, amaiera;
    private AudioSource bunkerIturria, basoIturria, efektuIturria;
    private HorrorShotgun eskopeta;
    private HorrorGameManager jokoa;
    private Rigidbody gorputza;
    private Transform jokalaria;
    private BoxCollider[] teilatuak;
    private AudioClip[] hormigoizkoUrratsak, basokoUrratsak, marruak;
    private readonly List<AudioSource> etsaienIturriak = new List<AudioSource>();
    private readonly List<float> hurrengoMarruak = new List<float>();
    private int azkenMunizioa, azkenBizitzak, azkenEtsaiak;
    private bool azkenBirkarga, azkenAmaiera;
    private float hurrengoUrratsa;
    private bool barruan;

    private AudioSource SortuIturria(string izena, bool begizta)
    {
        GameObject o = new GameObject(izena);
        o.transform.SetParent(transform, false);
        AudioSource a = o.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = begizta;
        a.spatialBlend = 0f;
        return a;
    }

    private static AudioClip[] Kargatu(string aurrizkia)
    {
        AudioClip[] guztiak = Resources.LoadAll<AudioClip>("HorrorAudioV13b");
        List<AudioClip> emaitza = new List<AudioClip>();
        foreach (AudioClip clip in guztiak)
            if (clip != null && clip.name.StartsWith(aurrizkia, System.StringComparison.Ordinal))
                emaitza.Add(clip);
        return emaitza.ToArray();
    }

    private void Awake()
    {
        bunkerIturria = SortuIturria("HB13b_Giro_Bunker", true);
        basoIturria = SortuIturria("HB13b_Giro_Baso", true);
        efektuIturria = SortuIturria("HB13b_Efektuak", false);
        bunkerIturria.volume = 0f;
        basoIturria.volume = 0f;
        hormigoizkoUrratsak = Kargatu("concrete_");
        basokoUrratsak = Kargatu("forest_");
        marruak = Kargatu("monster_");

        if (SceneManager.GetActiveScene().name == "Hasiera")
        {
            bunkerIturria.clip = menuGiroa;
            bunkerIturria.volume = 0.19f;
            if (menuGiroa != null) bunkerIturria.Play();
            return;
        }
        eskopeta = FindFirstObjectByType<HorrorShotgun>();
        jokoa = FindFirstObjectByType<HorrorGameManager>();
        HorrorPlayerMovement mugimendua = FindFirstObjectByType<HorrorPlayerMovement>();
        if (mugimendua != null)
        {
            jokalaria = mugimendua.transform;
            gorputza = mugimendua.GetComponent<Rigidbody>();
        }
        List<BoxCollider> zerrenda = new List<BoxCollider>();
        foreach (BoxCollider box in FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (box != null && box.name.StartsWith("HB_Teilatu_", System.StringComparison.Ordinal)) zerrenda.Add(box);
        teilatuak = zerrenda.ToArray();
        if (eskopeta != null)
        {
            azkenMunizioa = eskopeta.Loaded;
            azkenBirkarga = eskopeta.IsReloading;
        }
        if (jokoa != null)
        {
            azkenBizitzak = jokoa.Lives;
            azkenEtsaiak = jokoa.DefeatedCount;
        }
        bunkerIturria.clip = bunkerGiroa;
        basoIturria.clip = basoGiroa;
        if (bunkerGiroa != null) bunkerIturria.Play();
        if (basoGiroa != null) basoIturria.Play();

        HorrorEnemyHealth[] etsaiak = FindObjectsByType<HorrorEnemyHealth>(FindObjectsSortMode.None);
        foreach (HorrorEnemyHealth etsai in etsaiak)
        {
            AudioSource soinu = etsai.gameObject.AddComponent<AudioSource>();
            soinu.playOnAwake = false;
            soinu.loop = false;
            soinu.spatialBlend = 1f;
            soinu.rolloffMode = AudioRolloffMode.Logarithmic;
            soinu.minDistance = 1.5f;
            soinu.maxDistance = 16f;
            soinu.volume = .48f;
            etsaienIturriak.Add(soinu);
            hurrengoMarruak.Add(Time.time + Random.Range(4f, 12f));
        }
    }

    private bool Bunkerrean()
    {
        if (jokalaria == null || teilatuak == null) return false;
        Vector3 p = jokalaria.position;
        foreach (BoxCollider t in teilatuak)
        {
            if (t == null) continue;
            Bounds b = t.bounds;
            if (p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z && p.y < b.min.y)
                return true;
        }
        return false;
    }

    private void Jo(AudioClip clip, float bolumena)
    {
        if (clip != null && efektuIturria != null) efektuIturria.PlayOneShot(clip, bolumena);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "Hasiera") return;
        barruan = Bunkerrean();
        bunkerIturria.volume = Mathf.MoveTowards(bunkerIturria.volume, barruan ? .23f : 0f, Time.deltaTime * .11f);
        basoIturria.volume = Mathf.MoveTowards(basoIturria.volume, barruan ? 0f : .23f, Time.deltaTime * .11f);

        if (gorputza != null && jokoa != null && !jokoa.Ended && Time.time >= hurrengoUrratsa)
        {
            Vector3 v = gorputza.linearVelocity;
            float horizontal = new Vector2(v.x, v.z).magnitude;
            if (horizontal > .65f && Mathf.Abs(v.y) < 1.4f)
            {
                AudioClip[] multzoa = barruan ? hormigoizkoUrratsak : basokoUrratsak;
                AudioClip clip = multzoa.Length > 0 ? multzoa[Random.Range(0, multzoa.Length)] : pausoa;
                efektuIturria.pitch = Random.Range(.96f, 1.045f);
                Jo(clip, barruan ? .33f : .40f);
                efektuIturria.pitch = 1f;
                hurrengoUrratsa = Time.time + Random.Range(.51f, .66f);
            }
        }
        if (eskopeta != null)
        {
            if (eskopeta.Loaded < azkenMunizioa && !eskopeta.IsReloading) Jo(tiroa, .78f);
            if (eskopeta.IsReloading && !azkenBirkarga) Jo(birkarga, .63f);
            azkenMunizioa = eskopeta.Loaded;
            azkenBirkarga = eskopeta.IsReloading;
        }
        if (jokoa != null)
        {
            if (jokoa.Lives < azkenBizitzak && !jokoa.Ended) Jo(kolpea, .72f);
            if (jokoa.DefeatedCount > azkenEtsaiak && !jokoa.Ended) Jo(etsaia, .46f);
            if (jokoa.Ended && !azkenAmaiera)
                Jo(jokoa.DefeatedCount >= 3 ? amaiera : etsaia, .85f); // GARAIPENA: amaiera; PORROTA: marrua.
            azkenBizitzak = jokoa.Lives;
            azkenEtsaiak = jokoa.DefeatedCount;
            azkenAmaiera = jokoa.Ended;
        }
        if (jokalaria == null || jokoa == null || jokoa.Ended || marruak.Length == 0) return;
        for (int i = 0; i < etsaienIturriak.Count; ++i)
        {
            AudioSource src = etsaienIturriak[i];
            if (src == null || !src.gameObject.activeInHierarchy) continue;
            if (Time.time < hurrengoMarruak[i]) continue;
            if (Vector3.Distance(src.transform.position, jokalaria.position) < 19f)
                src.PlayOneShot(marruak[Random.Range(0, marruak.Length)], .68f);
            hurrengoMarruak[i] = Time.time + Random.Range(9f, 19f);
        }
    }
}
