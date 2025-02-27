using System;
using Game.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Panel
{
    public class LicensePanel : MonoBehaviour
    {
        public TMP_InputField licenseInputField;
        public Button submitButton;
        public Button copyDeviceIDButton;
        public GameObject loadingPanel;
        void Start()
        {
            submitButton.onClick.AddListener(SubmitLicense);
            copyDeviceIDButton.onClick.AddListener(CopyDeviceID);
            CheckLicense();
        }

        private void CopyDeviceID()
        {
            GUIUtility.systemCopyBuffer = SystemInfo.deviceUniqueIdentifier;
        }

        private void SubmitLicense()
        {
            var license = licenseInputField.text;
            LicenseManager.Instance.SaveLicense(license);
            licenseInputField.text = "";
            CheckLicense();
        }

        private void CheckLicense()
        {
            var checkLicenseValid = LicenseManager.Instance.CheckLicenseValid(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            if (checkLicenseValid)
            {
                gameObject.SetActive(false);
                loadingPanel.SetActive(true);
            }
        }
    }
}
