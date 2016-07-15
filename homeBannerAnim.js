(function($){

    var banner = {
        currentBanner: 0,
        animDir: 1,
        width: 1024,
        bar_width: 256,
        count: 4,
        interval: null,
        reset: function()
        {
            $(this).stop();
            this.stopAnimation();
            this.currentBanner = 0;
            this.animDir = 1;
            this.width = $("#banner").width();
            this.count = $("#banner").children().length;
            this.bar_width = this.width / this.count;
            $("#banner > a").first().css("margin-left", 0);
            $("#nav_bar").css("width", this.bar_width).css("margin-left", 0);
            this.startAnimation();
        },
        moveToBanner: function(index, duration)
        {
            $(this).stop();
            $(this).animate({ currentBanner: index },
            {
                duration: duration,
                easing: "swing",
                step: function (val)
                {
                    $("#banner > a").first().css("margin-left", val * this.width * -1);
                    $("#nav_bar").css("margin-left", val * this.bar_width);
                }
            });
        },
        startAnimation: function()
        {
            var _this = this;
            this.interval = setInterval(function ()
            {
                var nextBanner = _this.currentBanner;
                nextBanner += _this.animDir;
                if (nextBanner < 0)
                {
                    _this.animDir = 1;
                    nextBanner = 1;
                }
                else if (nextBanner > _this.count - 1)
                {
                    _this.animDir = -1;
                    nextBanner = _this.count - 2;
                }
                _this.moveToBanner(nextBanner, 1500);
            }, 10000);
        },
        stopAnimation: function()
        {
            clearInterval(this.interval);
        }
    };

    $(document).ready(function ()
    {
        $("#banner_nav").show();
        banner.reset();

        if (!isMobile())
        {
            $(window).resize(function ()
            {
                banner.reset();
            })
        };

        $("#banner_nav").click(function(e)
        {
            banner.stopAnimation();
            var clickX = (e.pageX - $(this).position().left);
            var nextBanner = Math.floor(clickX / banner.bar_width);
            banner.moveToBanner(nextBanner, 1000);
            banner.animDir = 1;
            banner.startAnimation();
        })

        $("#banner_nav, #banner").on("mouseenter", function ()
        {
            $("#banner_nav").css({"height": "12px", "border-bottom-width": "0"});
        }).on("mouseleave", function ()
        {
            $("#banner_nav").css({ "height": "6px", "border-bottom-width": "6px" });
        });
    });
    
}(jQuery));