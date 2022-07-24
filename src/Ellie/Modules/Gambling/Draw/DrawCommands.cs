﻿#nullable disable
using Ellie.Modules.Gambling.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Ellie.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public partial class DrawCommands : EllieModule
    {
        private static readonly ConcurrentDictionary<IGuild, Deck> _allDecks = new();
        private readonly IImageCache _images;

        public DrawCommands(IImageCache images)
            => _images = images;

        private async Task<(Stream ImageStream, string ToSend)> InternalDraw(int num, ulong? guildId = null)
        {
            if (num is < 1 or > 10)
                throw new ArgumentOutOfRangeException(nameof(num));

            var cards = guildId is null ? new() : _allDecks.GetOrAdd(ctx.Guild, _ => new());
            var images = new List<Image<Rgba32>>();
            var cardObjects = new List<Deck.Card>();
            for (var i = 0; i < num; i++)
            {
                if (cards.CardPool.Count == 0 && i != 0)
                {
                    try
                    {
                        await ReplyErrorLocalizedAsync(strs.no_more_cards);
                    }
                    catch
                    {
                        // ignored
                    }

                    break;
                }

                var currentCard = cards.Draw();
                cardObjects.Add(currentCard);
                var cardName = currentCard.ToString().ToLowerInvariant().Replace(' ', '_');
                images.Add(Image.Load(await File.ReadAllBytesAsync($"data/images/cards/{cardName}.jpg")));
            }

            using var img = images.Merge();
            foreach (var i in images)
                i.Dispose();

            var toSend = $"{Format.Bold(ctx.User.ToString())}";
            if (cardObjects.Count == 5)
                toSend += $" drew `{Deck.GetHandValue(cardObjects)}`";

            if (guildId is not null)
                toSend += "\n" + GetText(strs.cards_left(Format.Bold(cards.CardPool.Count.ToString())));

            return (img.ToStream(), toSend);
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task Draw(int num = 1)
        {
            if (num < 1)
                num = 1;
            if (num > 10)
                num = 10;

            var (imageStream, toSend) = await InternalDraw(num, ctx.Guild.Id);
            await using (imageStream)
            {
                await ctx.Channel.SendFileAsync(imageStream, num + " cards.jpg", toSend);
            }
        }

        [Cmd]
        public async partial Task DrawNew(int num = 1)
        {
            if (num < 1)
                num = 1;
            if (num > 10)
                num = 10;

            var (imageStream, toSend) = await InternalDraw(num);
            await using (imageStream)
            {
                await ctx.Channel.SendFileAsync(imageStream, num + " cards.jpg", toSend);
            }
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        public async partial Task DeckShuffle()
        {
            //var channel = (ITextChannel)ctx.Channel;

            _allDecks.AddOrUpdate(ctx.Guild,
                _ => new(),
                (_, c) =>
                {
                    c.Restart();
                    return c;
                });

            await ReplyConfirmLocalizedAsync(strs.deck_reshuffled);
        }
    }
}