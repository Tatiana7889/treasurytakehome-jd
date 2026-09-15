namespace Ttb.LabelReview.Api.Models;

/// <summary>
/// The values submitted on the COLA (Certificate of Label Approval) application,
/// as entered by the applicant. This is the "source of truth" the extracted
/// label text is compared against.
/// </summary>
public class ApplicationData
{
    public string BrandName { get; set; } = string.Empty;

    public string ClassType { get; set; } = string.Empty;

    /// <summary>
    /// Alcohol content as stated on the application, e.g. "13.5% ALC/VOL".
    /// Stored as free text since formatting conventions vary by beverage type.
    /// </summary>
    public string AlcoholContent { get; set; } = string.Empty;

    /// <summary>
    /// Net contents as stated on the application, e.g. "750 mL".
    /// </summary>
    public string NetContents { get; set; } = string.Empty;

    public string BottlerOrProducerName { get; set; } = string.Empty;

    public string BottlerOrProducerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Populated only when the product is imported. Left blank for
    /// domestically produced beverages.
    /// </summary>
    public string? CountryOfOrigin { get; set; }

    /// <summary>
    /// The exact Government Warning statement expected, per 27 CFR 16.21.
    /// Defaults to the standard statutory text but can be overridden if the
    /// applicant supplied a variant (e.g. bilingual labeling).
    /// </summary>
    public string GovernmentWarningText { get; set; } =
        "GOVERNMENT WARNING: (1) ACCORDING TO THE SURGEON GENERAL, WOMEN SHOULD NOT DRINK " +
        "ALCOHOLIC BEVERAGES DURING PREGNANCY BECAUSE OF THE RISK OF BIRTH DEFECTS. (2) " +
        "CONSUMPTION OF ALCOHOLIC BEVERAGES IMPAIRS YOUR ABILITY TO DRIVE A CAR OR OPERATE " +
        "MACHINERY, AND MAY CAUSE HEALTH PROBLEMS.";

    /// <summary>
    /// Beverage category drives which fields are mandatory (e.g. alcohol
    /// content disclosure rules differ for beer vs. wine vs. spirits).
    /// </summary>
    public BeverageType BeverageType { get; set; } = BeverageType.DistilledSpirits;
}

public enum BeverageType
{
    Beer = 0,
    Wine = 1,
    DistilledSpirits = 2
}

