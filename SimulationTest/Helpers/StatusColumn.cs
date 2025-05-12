using Spectre.Console;
using Spectre.Console.Rendering;

namespace SimulationTest.Helpers
{
    /// <summary>
    /// A custom column for displaying status information in a progress bar
    /// </summary>
    public class StatusColumn : ProgressColumn
    {
        private readonly int _maxWidth;

        /// <summary>
        /// Initializes a new instance of the StatusColumn class
        /// </summary>
        /// <param name="maxWidth">Maximum width of the status text</param>
        public StatusColumn(int maxWidth = 40)
        {
            _maxWidth = maxWidth;
        }

        /// <inheritdoc/>
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            string text = task.Description ?? string.Empty;

            // Limit the length of the status text
            if (text.Length > _maxWidth)
            {
                text = text.Substring(0, _maxWidth - 3) + "...";
            }

            return new Markup(text);
        }
    }
}