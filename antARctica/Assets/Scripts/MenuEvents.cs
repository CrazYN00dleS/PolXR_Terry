﻿using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.IO;

public class MenuEvents : MonoBehaviour
{
    // The current menu type, true stands for main menu.
    private bool MainMenu;

    // Initially set to an empty object to avoid null reference.
    public Transform radarImage;
    public Transform RadarImagesContainer;
    private Transform CSVPicks;
    public Transform CSVPicksContainer;
    public Transform SurfaceDEM;
    public Transform BaseDEM;
    public BoxCollider AntarcticaBoxCollider;


    // The data needed for smoothing the menu movement.
    private Vector3 targetPosition;
    private Vector3 targetScale = new Vector3(1.0f, 1.0f, 1.0f);
    private bool updatePosition = false;
    public bool moveMenu = false;

    // The sliders.
    public PinchSlider horizontalSlider;
    public PinchSlider verticalSlider;
    public PinchSlider rotationSlider;
    public PinchSlider transparencySlider;
    public PinchSlider verticalExaggerationSlider;

    // Radar Menu Toggle Buttons
    public Interactable RadarToggle;
    public Interactable CSVPicksToggle;
    public Interactable AllRadarToggle;
    public Interactable AllCSVPicksToggle;
    public Interactable SurfaceDEMToggle;
    public Interactable BaseDEMToggle;
    public Interactable BoundingBoxToggle;

    // The initial scale, rotation and position of the radar image.
    public Vector3 originalScale;
    private Vector3 originalRotation;
    private Vector3 originalPosition;
    public float scaleX;
    public float scaleY;

    // The scale for calculating the text value
    public float scale = 1000;

    // Dimension calculations.
    private float OriginalHeight;
    private float OriginalWidth;
    private float ScaledHeight;
    private float ScaledWidth;
    private float StrainHeight;
    private float StrainWidth;

    // Text objects
    public TextMeshPro Title;
    public TextMeshPro VerticalTMP;
    public TextMeshPro HorizontalTMP;
    public TextMeshPro RotationDegreeTMP;
    public TextMeshPro TransparencyTMP;
    public TextMeshPro MarkTMP;

    // The information needed for updating the selected point coordinates.
    public GameObject MarkObj;
    public string SelectionDialog = "Assets/dialog.txt";
    private float yOrigin = 1.75f / 5.5f;

    void Start()
    {
        // Deactivate the radar menu before any selection happens.
        HomeButton(true);
    }

    // Update is called once per frame
    void Update()
    {
        // The starting animation for menu.
        if (moveMenu)
        {
            if (Vector3.Distance(targetPosition, this.transform.position) > 2) updatePosition = true;
            else if (Vector3.Distance(targetPosition, this.transform.position) < 0.01f) updatePosition = false;
            if (updatePosition) this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, 0.5f);
            this.transform.rotation = Quaternion.Lerp(this.transform.rotation, Camera.main.transform.rotation, 0.01f);
        }
        this.transform.localScale = Vector3.Lerp(this.transform.localScale, targetScale, 0.5f);
        if (this.transform.localScale.x < 0.1f) this.gameObject.SetActive(false);

        if (MainMenu)
        {
            // Main Menu Toggling Objects 
            RadarImagesContainer.gameObject.SetActive(AllRadarToggle.IsToggled);
            SurfaceDEM.gameObject.SetActive(SurfaceDEMToggle.IsToggled);
            BaseDEM.gameObject.SetActive(BaseDEMToggle.IsToggled);
            AllLinesOn(AllCSVPicksToggle.IsToggled);
            AntarcticaBoxCollider.enabled = BoundingBoxToggle.IsToggled;
        }
        else
        {
            // Radar Menu Toggling Objects
            radarImage.gameObject.SetActive(RadarToggle.IsToggled);
            CSVPicks.localScale = CSVPicksToggle.IsToggled? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);

            // Update the slider value accordingly.
            Vector3 currentScale = radarImage.localScale;
            horizontalSlider.SliderValue = currentScale.x / originalScale.x - 0.5f;
            verticalSlider.SliderValue = currentScale.y / originalScale.y - 0.5f;
            float rounded_angle = (float)((radarImage.rotation.eulerAngles.y - originalRotation.y) / 359.9);
            rotationSlider.SliderValue = rounded_angle >= 0 ? rounded_angle : rounded_angle + 360.0f;

            // Set original scale values & coefficients
            float updatedScaleX = radarImage.localScale.x;
            float updatedScaleY = radarImage.localScale.y;

            // Get current dimensions of the radar image
            ScaledHeight = updatedScaleY * scale;
            ScaledWidth = updatedScaleX * scale;

            // Calculate strain
            StrainHeight = Math.Abs(OriginalHeight - ScaledHeight);
            StrainWidth = Math.Abs(OriginalWidth - ScaledWidth);

            // Set scaled dimensions text
            VerticalTMP.text = string.Format(
                "Original:   {0} m \n" +
                "Current:    {1} m \n" +
                "Strain:     {2}",
                OriginalHeight.ToString(), ScaledHeight.ToString(), StrainHeight.ToString());

            // going to need a database for this/some spreadsheet with the values
            HorizontalTMP.text = string.Format(
                "Original:   {0} m \n" +
                "Current:    {1} m \n" +
                "Strain:     {2}",
                OriginalWidth.ToString(), ScaledWidth.ToString(), StrainWidth.ToString());

            // Set rotation text
            RotationDegreeTMP.text = string.Format("ROTATION:      {0}°", radarImage.localEulerAngles.y.ToString());

            // Set rotation text
            TransparencyTMP.text = string.Format("Transparency:      {0}%", transparencySlider.SliderValue * 100);

            // Update the selected point coordinates
            float maxX = MarkObj.transform.parent.gameObject.transform.localScale.x * 10000;
            float maxY = MarkObj.transform.parent.gameObject.transform.localScale.y * 100;
            float radarX = (MarkObj.transform.localPosition.x + 0.5f) * maxX;
            float radarY = (MarkObj.transform.localPosition.y - yOrigin) * maxY;

            if (MarkObj.transform.parent.name != "Antarctica")
                MarkTMP.text = string.Format(
                    "{0}: ({1}, {2})\n" +
                    "X: {3}, Y: {4}",
                    MarkObj.transform.parent.name, radarX.ToString(), radarY.ToString(), maxX.ToString(), maxY.ToString());
            else
                MarkTMP.text = "No selected points.";
        }
    }

    // Reset the original radar image transform and re-assign the new radar image.
    public void ResetRadar(Transform newRadar, Vector3 newPosition, float newAlpha)
    {
        MainMenu = false;
        this.transform.Find("RadarMenu").gameObject.SetActive(true);
        this.transform.Find("MainMenu").gameObject.SetActive(false);

        targetPosition = newPosition;

        if (radarImage != newRadar)
        {
            // This reset may not be needed depending on the design.
            if (radarImage != null && radarImage.name != "Radar Image Placeholder")
            {
                transparencySlider.SliderValue = newAlpha;
                radarImage.gameObject.SetActive(true);
                CSVPicks.gameObject.transform.localScale = new Vector3(1, 1, 1);
                ResetButton();
            }

            // Switch to new radar and reset the values.
            radarImage = newRadar;
            CSVPicks = CSVPicksContainer.Find(radarImage.name);
            originalScale = radarImage.localScale;
            originalRotation = radarImage.rotation.eulerAngles;
            originalPosition = radarImage.position;

            // Set the title of the menu to the current radar.
            Title.text = radarImage.name;

            // Set original dimension values
            OriginalHeight = originalScale.y * scale;
            OriginalWidth = originalScale.x * scale;
        }
    }

    // The reset button for the radarImage transform.
    public void ResetButton()
    {
        radarImage.position = originalPosition;
        radarImage.rotation = Quaternion.Euler(originalRotation);
        radarImage.localScale = originalScale;
        radarImage.GetComponent<RadarEvents>().SetAlpha(1);
        transparencySlider.SliderValue = 0;
        RadarToggle.IsToggled = true;
        CSVPicksToggle.IsToggled = true;
    }

    // The write button for writting the coordinates into a file.
    // Reference https://forum.unity.com/threads/how-to-write-a-file.8864/
    // Be aware of the file path issue! And try to keep a history...
    public void WriteButton()
    {
        if (File.Exists(SelectionDialog))
        {
            List<string> tempList = new List<string> { MarkTMP.text };
            File.AppendAllLines(SelectionDialog, tempList);
        }
        else
        {
            var sr = File.CreateText(SelectionDialog);
            sr.WriteLine(MarkTMP.text);
            sr.Close();
        }
    }

    // The close button, make the menu disappear and deactivated.
    public void CloseButton(bool shutDown)
    {
        if (shutDown) targetScale = new Vector3(0.0f, 0.0f, 0.0f);
        else
        {
            targetScale = new Vector3(1.0f, 1.0f, 1.0f);
            if (this.transform.localScale.x < 0.1f) this.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            this.gameObject.SetActive(true);
        }
    }

    public void HomeButton(bool home)
    {
        MainMenu = home;
        this.transform.Find("RadarMenu").gameObject.SetActive(!home);
        this.transform.Find("MainMenu").gameObject.SetActive(home);
    }

    // The four slider update interface.
    public void OnVerticalSliderUpdated(SliderEventData eventData)
    {
        scaleY = 0.5f + eventData.NewValue;
        radarImage.localScale = new Vector3(originalScale.x * scaleX, originalScale.y * scaleY, originalScale.z);
    }

    public void OnHorizontalSliderUpdated(SliderEventData eventData)
    {
        scaleX = 0.5f + eventData.NewValue;
        radarImage.localScale = new Vector3(originalScale.x * scaleX, originalScale.y * scaleY, originalScale.z);
    }

    public void OnRotateSliderUpdated(SliderEventData eventData)
    {
        float rotate = (float)(359.9 * eventData.NewValue);
        radarImage.localRotation = Quaternion.Euler(0, rotate, 0);
    }

    public void OnTransparencySliderUpdated(SliderEventData eventData)
    {
        if (radarImage != null && radarImage.name != "Radar Image Placeholder")
            radarImage.GetComponent<RadarEvents>().SetAlpha(1 - eventData.NewValue);
    }

    // Main Menu Vertical Exaggeration Slider
    public void OnVerticalExaggerationSliderUpdated(SliderEventData eventData)
    {
        SurfaceDEM.localScale = new Vector3(1, 0.5f + eventData.NewValue, 1);
        BaseDEM.localScale = new Vector3(1, 0.5f + eventData.NewValue, 1);
    }

    // Main Menu Toggling CSV lines
    public void AllLinesOn(bool input)
    {
        Vector3 newScale = input ? new Vector3(1, 1, 1) : new Vector3(0, 0, 0);
        int children = CSVPicksContainer.childCount;
        for (int i = 0; i < children; ++i)
            CSVPicksContainer.GetChild(i).localScale = newScale;
    }
}