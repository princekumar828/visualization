const echarts = require('echarts');
const fs = require('fs');

// Read input from stdin
let inputData = '';

process.stdin.on('data', chunk => {
    inputData += chunk;
});

process.stdin.on('end', () => {
    try {
        if (!inputData) {
            console.error('No input data received');
            process.exit(1);
        }

        const data = JSON.parse(inputData);

        // Initialize chart with SVG renderer and SSR mode
        // Note: For SSR, we don't need a DOM container, just width/height
        const chart = echarts.init(null, null, {
            renderer: 'svg',
            ssr: true,
            width: data.width || 800,
            height: data.height || 600
        });

        // Set options
        chart.setOption(data.option);

        // Render to SVG string
        const svgStr = chart.renderToSVGString();

        // Output to stdout
        process.stdout.write(svgStr);

        // Clean up
        chart.dispose();
        process.exit(0);

    } catch (error) {
        console.error('Error rendering chart:', error.message);
        process.exit(1);
    }
});
