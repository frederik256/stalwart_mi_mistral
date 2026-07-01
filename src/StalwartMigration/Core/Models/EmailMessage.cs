// <copyright file="EmailMessage.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StalwartMigration.Core.Models;

/// <summary>
/// Represents an email message.
/// </summary>
public class EmailMessage : IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    [JsonPropertyName("messageId")]
    [Required(ErrorMessage = "Message ID is required.")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject of the email.
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    [JsonPropertyName("from")]
    [Required(ErrorMessage = "From address is required.")]
    public EmailAddress From { get; set; } = new();

    /// <summary>
    /// Gets or sets the recipient email addresses (To).
    /// </summary>
    [JsonPropertyName("to")]
    public List<EmailAddress> To { get; set; } = new();

    /// <summary>
    /// Gets or sets the CC recipient email addresses.
    /// </summary>
    [JsonPropertyName("cc")]
    public List<EmailAddress> Cc { get; set; } = new();

    /// <summary>
    /// Gets or sets the BCC recipient email addresses.
    /// </summary>
    [JsonPropertyName("bcc")]
    public List<EmailAddress> Bcc { get; set; } = new();

    /// <summary>
    /// Gets or sets the reply-to email address.
    /// </summary>
    [JsonPropertyName("replyTo")]
    public EmailAddress? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the email was sent.
    /// </summary>
    [JsonPropertyName("date")]
    [Required(ErrorMessage = "Date is required.")]
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the email was received.
    /// </summary>
    [JsonPropertyName("receivedAt")]
    public DateTimeOffset? ReceivedAt { get; set; }

    /// <summary>
    /// Gets or sets the size of the email in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    [Range(0, long.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets whether the email has been read.
    /// </summary>
    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets whether the email is flagged.
    /// </summary>
    [JsonPropertyName("isFlagged")]
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Gets or sets whether the email has been answered.
    /// </summary>
    [JsonPropertyName("isAnswered")]
    public bool IsAnswered { get; set; }

    /// <summary>
    /// Gets or sets whether the email has been forwarded.
    /// </summary>
    [JsonPropertyName("isForwarded")]
    public bool IsForwarded { get; set; }

    /// <summary>
    /// Gets or sets whether the email is a draft.
    /// </summary>
    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; set; }

    /// <summary>
    /// Gets or sets the priority of the email.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    /// <summary>
    /// Gets or sets the email body text content.
    /// </summary>
    [JsonPropertyName("bodyText")]
    public string? BodyText { get; set; }

    /// <summary>
    /// Gets or sets the email body HTML content.
    /// </summary>
    [JsonPropertyName("bodyHtml")]
    public string? BodyHtml { get; set; }

    /// <summary>
    /// Gets or sets the email headers as a dictionary.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the attachments for the email.
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<EmailAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Gets or sets the folder where the email is stored.
    /// </summary>
    [JsonPropertyName("folder")]
    public string? Folder { get; set; }

    /// <summary>
    /// Gets or sets the account ID that owns this email.
    /// </summary>
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the mailbox ID where the email is stored.
    /// </summary>
    [JsonPropertyName("mailboxId")]
    public string? MailboxId { get; set; }

    /// <summary>
    /// Gets or sets the in-reply-to message ID for threaded conversations.
    /// </summary>
    [JsonPropertyName("inReplyTo")]
    public string? InReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the references message IDs for threaded conversations.
    /// </summary>
    [JsonPropertyName("references")]
    public List<string> References { get; set; } = new();

    /// <summary>
    /// Gets the total size of all attachments.
    /// </summary>
    [JsonIgnore]
    public long AttachmentsSize => Attachments.Sum(a => a.Size);

    /// <summary>
    /// Gets whether this email has attachments.
    /// </summary>
    [JsonIgnore]
    public bool HasAttachments => Attachments.Count > 0;

    /// <summary>
    /// Gets the total number of recipients (To + Cc + Bcc).
    /// </summary>
    [JsonIgnore]
    public int TotalRecipients => To.Count + Cc.Count + Bcc.Count;

    /// <summary>
    /// Gets the body content (preferring HTML over text).
    /// </summary>
    [JsonIgnore]
    public string? Body => !string.IsNullOrEmpty(BodyHtml) ? BodyHtml : BodyText;

    /// <summary>
    /// Initializes a new instance of the EmailMessage class.
    /// </summary>
    public EmailMessage()
    { }

    /// <summary>
    /// Initializes a new instance of the EmailMessage class with the specified message ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    public EmailMessage(string messageId)
    {
        MessageId = messageId;
    }

    /// <summary>
    /// Validates the email message model.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate message ID
        if (string.IsNullOrWhiteSpace(MessageId))
        {
            yield return new ValidationResult(
                "Message ID is required.",
                [nameof(MessageId)]);
        }

        // Validate from address
        if (From == null || string.IsNullOrWhiteSpace(From.Address))
        {
            yield return new ValidationResult(
                "From address is required.",
                [nameof(From)]);
        }

        // Validate date
        if (Date == default)
        {
            yield return new ValidationResult(
                "Date is required.",
                [nameof(Date)]);
        }

        // Validate size
        if (Size < 0)
        {
            yield return new ValidationResult(
                "Size cannot be negative.",
                [nameof(Size)]);
        }
    }

    /// <summary>
    /// Validates the email message and throws an exception if invalid.
    /// </summary>
    public void ValidateAndThrow()
    {
        var validationResults = Validate(new ValidationContext(this)).ToList();

        if (validationResults.Count > 0)
        {
            throw new ValidationException(string.Join("\n", validationResults.Select(r => r.ErrorMessage)));
        }
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        return $"[{Date:yyyy-MM-dd}] {From.Address} -> {string.Join(", ", To.Select(t => t.Address))}: {Subject ?? "(No Subject)"}";
    }
}

/// <summary>
/// Represents an email address with optional display name.
/// </summary>
public class EmailAddress : IEquatable<EmailAddress>
{
    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Initializes a new instance of the EmailAddress class.
    /// </summary>
    public EmailAddress()
    { }

    /// <summary>
    /// Initializes a new instance of the EmailAddress class with the specified address.
    /// </summary>
    /// <param name="address">The email address.</param>
    public EmailAddress(string address)
    {
        Address = address;
    }

    /// <summary>
    /// Initializes a new instance of the EmailAddress class with the specified address and display name.
    /// </summary>
    /// <param name="address">The email address.</param>
    /// <param name="displayName">The display name.</param>
    public EmailAddress(string address, string displayName)
    {
        Address = address;
        DisplayName = displayName;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public bool Equals(EmailAddress? other)
    {
        if (other == null)
        {
            return false;
        }

        return string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as EmailAddress);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode()
    {
        return Address.ToLowerInvariant().GetHashCode();
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        return !string.IsNullOrEmpty(DisplayName) ? $"{DisplayName} <{Address}>" : Address;
    }
}

/// <summary>
/// Represents an email attachment.
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// Gets or sets the attachment identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the file name of the attachment.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type of the attachment.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the size of the attachment in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    [Range(0, long.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the content ID of the attachment.
    /// </summary>
    [JsonPropertyName("contentId")]
    public string? ContentId { get; set; }

    /// <summary>
    /// Gets or sets the content disposition of the attachment.
    /// </summary>
    [JsonPropertyName("contentDisposition")]
    public string? ContentDisposition { get; set; }

    /// <summary>
    /// Gets or sets the file path where the attachment is stored.
    /// </summary>
    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the checksum of the attachment content.
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }

    /// <summary>
    /// Initializes a new instance of the EmailAttachment class.
    /// </summary>
    public EmailAttachment()
    { }

    /// <summary>
    /// Initializes a new instance of the EmailAttachment class with the specified file name and size.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="size">The size in bytes.</param>
    public EmailAttachment(string fileName, long size)
    {
        FileName = fileName;
        Size = size;
    }
}

/// <summary>
/// Represents the priority of an email message.
/// </summary>
public enum EmailPriority
{
    /// <summary>
    /// Low priority.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal = 3,

    /// <summary>
    /// High priority.
    /// </summary>
    High = 5
}
