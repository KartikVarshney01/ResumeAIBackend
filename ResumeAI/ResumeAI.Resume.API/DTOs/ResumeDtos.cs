using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Resume.API.DTOs;

public class CreateResumeDto
{
    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? TargetJobTitle { get; set; }

    public int TemplateId { get; set; }

    public string Language { get; set; } = "en";
}

public class UpdateResumeDto
{
    [MaxLength(150)]
    public string? Title { get; set; }

    public string? TargetJobTitle { get; set; }

    public int? TemplateId { get; set; }

    public string? Status { get; set; }

    public string? Language { get; set; }
}

public class UpdateAtsScoreDto
{
    [Required, Range(0, 100)]
    public int Score { get; set; }
}

public class ReorderSectionsDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public IList<int> OrderedIds { get; set; } = new List<int>();
}

public class CreateSectionDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public string SectionType { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;
}

public class UpdateSectionDto
{
    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? SectionType { get; set; }
}

public class BulkUpdateSectionsDto
{
    [Required]
    public int ResumeId { get; set; }

    [Required]
    public IList<UpdateSectionItemDto> Sections { get; set; } = new List<UpdateSectionItemDto>();
}

public class UpdateSectionItemDto
{
    [Required]
    public int SectionId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public bool? IsVisible { get; set; }

    public int? DisplayOrder { get; set; }
}