﻿using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModernFirearmKitMod
{
    public class AssetManager : SingleInstance<AssetManager>
    {
        public override string Name { get; } = "Asset Manager";

        public Asset_Rocket Rocket { get; private set; }
        public Asset_Explosion Explosion { get; private set; }
        public Asset_MachineGun MachineGun { get; private set; }
        public Asset_GatlingGun GatlingGun { get; private set; }
        public Asset_Bullet Bullet { get; private set; }

        private void Awake()
        {
            ModResource.CreateAssetBundleResource("Effect", @"Resources/bundle_mfk");
            StartCoroutine(LoadAssetBundle());
            SceneManager.sceneLoaded += (s, a) => { StartCoroutine(LoadAssetBundle()); };

        }

        private IEnumerator LoadAssetBundle()
        {         
            ModAssetBundle modAssetBundle = ModResource.GetAssetBundle("Effect");
            yield return new WaitUntil(() => modAssetBundle.Available);
            Rocket = new Asset_Rocket(modAssetBundle);
            Explosion = new Asset_Explosion(modAssetBundle);
            MachineGun = new Asset_MachineGun(modAssetBundle);
            GatlingGun = new Asset_GatlingGun(modAssetBundle);
            Bullet = new Asset_Bullet(modAssetBundle);
        }     
    }

    public class Asset_Rocket
    {
        public GameObject rocketTrailEffect;
        public GameObject rocketTemp;

        public Asset_Rocket(ModAssetBundle modAssetBundle)
        {
            rocketTrailEffect = modAssetBundle.LoadAsset<GameObject>("RocketTrailEffect");
            rocketTemp = CreateRocketBlockTemp();
        }

        private static GameObject CreateRocketBlockTemp()
        {
            GameObject RocketTemp = new GameObject("Rocket Temp");
            RocketTemp.transform.localScale = new Vector3(1f, 0.75f, 0.75f);

            GameObject vis = new GameObject("Vis");
            vis.transform.SetParent(RocketTemp.transform);
            vis.transform.localPosition -= RocketTemp.transform.right;
            vis.transform.localScale = RocketTemp.transform.localScale;
            vis.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("RocketPod_r Mesh");
            vis.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("RocketPod_r Texture");

            GameObject collider = new GameObject("Collider");
            collider.transform.SetParent(RocketTemp.transform);
            collider.transform.localScale = RocketTemp.transform.localScale;
            CapsuleCollider capsuleCollider = collider.AddComponent<CapsuleCollider>();
            capsuleCollider.radius = 0.15f;
            capsuleCollider.height = 2.5f;
            capsuleCollider.direction = 0;
            capsuleCollider.isTrigger = true;

            Rigidbody rigidbody = RocketTemp.AddComponent<Rigidbody>();
            rigidbody.mass = 0.25f;

            RocketTemp.AddComponent<RocketScript>();
            RocketTemp.AddComponent<DestroyIfEditMode>();
            RocketTemp.SetActive(false);
            return RocketTemp;
        }
    }
    public class Asset_Explosion 
    {
        //爆炸特效
        public GameObject explosionEffect;

        public Asset_Explosion(ModAssetBundle modAssetBundle)
        {
            explosionEffect = modAssetBundle.LoadAsset<GameObject>("BigExplosion");
            explosionEffect.AddComponent<DestroyIfEditMode>();
        }
    }

    public class Asset_GatlingGun
    {
        //开火特效
        public GameObject fireEffect;
        //机枪过热材质
        public Material material;
        //子弹模板
        public GameObject bulletTemp;

        public Asset_GatlingGun(ModAssetBundle modAssetBundle)
        {
            material = modAssetBundle.LoadAsset<Material>("GatlingGunMat.mat");
            fireEffect = modAssetBundle.LoadAsset<GameObject>("MachineGunFireEffect");

            #region init fire effect
            fireEffect.AddComponent<DestroyIfEditMode>();

            Reactivator fPSDemo = fireEffect.AddComponent<Reactivator>();
            for (int i = 0; i < fireEffect.transform.childCount; i++)
            {
                var go = fireEffect.transform.GetChild(i).gameObject;
                if (go.name == "Point light")
                {
                    FPSLightCurves fPSLight = go.AddComponent<FPSLightCurves>();
                    fPSLight.GraphTimeMultiplier = 0.15f;
                    fPSLight.GraphIntensityMultiplier = 1;
                    Vector3 vector3 = go.transform.localPosition;
                    vector3.z = -0.5f;
                    go.transform.localPosition = vector3;
                }
                else if (go.name == "MuzzleFlash0")
                {
                    go.AddComponent<FPSRandomRotateAngle>().RotateZ = true;
                    go.transform.localScale = new Vector3(0.6f, 0.6f, 0f);
                }
                else if (go.name == "MuzzleFlash")
                {
                    go.transform.localPosition = new Vector3(0, 0, -0.2f);
                    go.transform.localScale = new Vector3(0.1f, 0.1f, 0.065f);
                }
                else if (go.name == "Distortion")
                {
                    go.transform.localPosition = new Vector3(0, 0, -0.5f);
                }
            }
            #endregion

            bulletTemp = CreateBulletTemp();
        }

        private GameObject CreateBulletTemp()
        {
            GameObject temp = new GameObject("Bullet Temp");

            temp.AddComponent<DestroyIfEditMode>();

            GameObject vis = new GameObject("Vis");
            vis.transform.SetParent(temp.transform);
            vis.transform.position = temp.transform.position;
            vis.transform.rotation = temp.transform.rotation;
            vis.transform.localScale *= 0.025f;

            vis.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("Bullet Mesh");
            MeshRenderer meshRenderer = vis.AddComponent<MeshRenderer>();
            meshRenderer.material.mainTexture = ModResource.GetTexture("Bullet Texture");

            Rigidbody rigidbody = temp.AddComponent<Rigidbody>();
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.mass = 0.5f;
            rigidbody.drag = 0.25f;

            TrailRenderer trailRenderer = temp.AddComponent<TrailRenderer>();
            trailRenderer.startWidth = 0.08f;
            trailRenderer.endWidth = 0f;
            trailRenderer.time = 0.05f;
            trailRenderer.material.shader = Shader.Find("Particles/Additive");
            trailRenderer.material.SetColor("_TintColor", Color.yellow);

            BoxCollider boxCollider = temp.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(0.1f, 0.1f, 0.25f);
            boxCollider.material.bounciness = 0f;

            BulletScript bulletScript = temp.AddComponent<BulletScript>();
            bulletScript.Direction = Vector3.forward;
            bulletScript.CollisionEnableTime = 1f;

            TimedSelfDestruct tsd = temp.AddComponent<TimedSelfDestruct>();

            temp.SetActive(false);
            return temp;
        }
    }
    public class Asset_MachineGun
    {
        //开火特效
        public GameObject fireEffect;
        //机枪过热材质
        public Material material;
        //子弹模板
        public GameObject bulletTemp;

        public Asset_MachineGun(ModAssetBundle modAssetBundle)
        {
            material = modAssetBundle.LoadAsset<Material>("MachineGunMat.mat");
            fireEffect = modAssetBundle.LoadAsset<GameObject>("MachineGunFireEffect");
     
            #region init fire effect
            fireEffect.AddComponent<DestroyIfEditMode>();

            Reactivator fPSDemo = fireEffect.AddComponent<Reactivator>();
            for (int i = 0; i < fireEffect.transform.childCount; i++)
            {
                var go = fireEffect.transform.GetChild(i).gameObject;
                if (go.name == "Point light")
                {
                    FPSLightCurves fPSLight = go.AddComponent<FPSLightCurves>();
                    fPSLight.GraphTimeMultiplier = 0.15f;
                    fPSLight.GraphIntensityMultiplier = 1;
                    Vector3 vector3 = go.transform.localPosition;
                    vector3.z = -0.5f;
                    go.transform.localPosition = vector3;
                }
                else if (go.name == "MuzzleFlash0")
                {
                    go.AddComponent<FPSRandomRotateAngle>().RotateZ = true;
                    go.transform.localScale = new Vector3(0.6f, 0.6f, 0f);
                }
                else if (go.name == "MuzzleFlash")
                {
                    go.transform.localPosition = new Vector3(0, 0, -0.2f);
                    go.transform.localScale = new Vector3(0.1f, 0.1f, 0.065f);
                }
                else if (go.name == "Distortion")
                {
                    go.transform.localPosition = new Vector3(0, 0, -0.5f);
                }
            }
            #endregion

            bulletTemp = CreateBulletTemp();
        }

        private GameObject CreateBulletTemp()
        {
            GameObject temp = new GameObject("Bullet Temp");
           
            temp.AddComponent<DestroyIfEditMode>();

            GameObject vis = new GameObject("Vis");
            vis.transform.SetParent(temp.transform);
            vis.transform.position = temp.transform.position;
            vis.transform.rotation = temp.transform.rotation;
            vis.transform.localScale *= 0.025f; 

            vis.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("Bullet Mesh");
            MeshRenderer meshRenderer = vis.AddComponent<MeshRenderer>();
            meshRenderer.material.mainTexture = ModResource.GetTexture("Bullet Texture");

            Rigidbody rigidbody = temp.AddComponent<Rigidbody>();
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.mass = 0.5f;
            rigidbody.drag = 0.25f;

            TrailRenderer trailRenderer = temp.AddComponent<TrailRenderer>();
            trailRenderer.startWidth = 0.08f;
            trailRenderer.endWidth = 0f;
            trailRenderer.time = 0.05f;
            trailRenderer.material.shader = Shader.Find("Particles/Additive");
            trailRenderer.material.SetColor("_TintColor", Color.yellow);

            BoxCollider boxCollider = temp.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(0.1f, 0.1f, 0.25f);
            boxCollider.material.bounciness = 0f;          

            BulletScript bulletScript = temp.AddComponent<BulletScript>();
            bulletScript.Direction = Vector3.forward;
            bulletScript.CollisionEnableTime = 1f;

            TimedSelfDestruct tsd = temp.AddComponent<TimedSelfDestruct>();

            temp.SetActive(false);
            return temp;
        }
    }

    public class Asset_Bullet
    {
        public GameObject impactWoodEffect;
        public GameObject impactStoneEffect;
        public GameObject impactMetalEffect;


        public Asset_Bullet(ModAssetBundle modAssetBundle)
        {
            impactWoodEffect = modAssetBundle.LoadAsset<GameObject>("BulletImpactWoodEffect");
            impactStoneEffect = modAssetBundle.LoadAsset<GameObject>("BulletImpactStoneEffect");
            impactMetalEffect = modAssetBundle.LoadAsset<GameObject>("BulletImpactMetalEffect");

            //impactWoodEffect.AddComponent<TimedSelfDestruct>().lifeTime = 50f;
            //impactStoneEffect.AddComponent<TimedSelfDestruct>().lifeTime = 50f;
            //impactMetalEffect.AddComponent<TimedSelfDestruct>().lifeTime = 50f;

        }

    }
}
